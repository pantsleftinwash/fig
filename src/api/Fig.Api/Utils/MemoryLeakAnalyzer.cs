using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public class MemoryLeakAnalyzer : IMemoryLeakAnalyzer
{
    public MemoryUsageAnalysisBusinessEntity? AnalyzeMemoryUsage(ClientRunSessionBusinessEntity runSession)
    {
        if (!IsEligibleForMemoryLeakCheck(runSession))
            return null;
        
        // We skip the first 10 records to avoid any volatility during start up.
        var allValidRecords = runSession.HistoricalMemoryUsage.OrderBy(a => a.ClientRunTimeSeconds)
            .Skip(10)
            .ToList();

        var (average, stdDev, startingAvg, endingAvg) = AnalyzeData(allValidRecords.Select(a => a.MemoryUsageBytes).ToList());

        var validRange = GetValidRange(average, stdDev);
        var recordsToCheck = RemoveOutliers(allValidRecords, validRange).ToList();

        return new MemoryUsageAnalysisBusinessEntity
        {
            TimeOfAnalysisUtc = DateTime.UtcNow,
            TrendLineSlope = GetTrendLine(recordsToCheck),
            Average = average,
            StandardDeviation = stdDev,
            StartingBytesAverage = startingAvg,
            EndingBytesAverage = endingAvg,
            SecondsAnalyzed = Convert.ToInt32(recordsToCheck.Last().ClientRunTimeSeconds - recordsToCheck.First().ClientRunTimeSeconds),
            DataPointsAnalyzed = recordsToCheck.Count
        };
    }

    private IEnumerable<MemoryUsageBusinessEntity> RemoveOutliers(IEnumerable<MemoryUsageBusinessEntity> records,
        (double Min, double Max) validRange)
    {
        return records.Where(record =>
            record.MemoryUsageBytes <= validRange.Max && 
            record.MemoryUsageBytes >= validRange.Min);
    }

    private bool IsEligibleForMemoryLeakCheck(ClientRunSessionBusinessEntity runSession)
    {
        if (runSession.MemoryAnalysis?.PossibleMemoryLeakDetected == true)
            return false;

        if (IsWithinFirstTwentyFiveMinutesOfRuntime())
            return false; // We wait 25 minutes before our first check.

        if (IsLessThanTwentyMinutesSinceLastCheck())
            return false; // Subsequent tests every 20 minutes

        if (!HasSufficientDataPoints())
            return false;
        
        return true;

        bool IsWithinFirstTwentyFiveMinutesOfRuntime() =>
            runSession.MemoryAnalysis is null &&
            runSession.UptimeSeconds < TimeSpan.FromMinutes(20).TotalSeconds;

        bool IsLessThanTwentyMinutesSinceLastCheck() =>
            runSession.MemoryAnalysis is not null &&
            DateTime.UtcNow - runSession.MemoryAnalysis.TimeOfAnalysisUtc < TimeSpan.FromMinutes(20);

        bool HasSufficientDataPoints() => runSession.HistoricalMemoryUsage.Count > 40;
    }

    private static (double average, double stdDev, double startingAvg, double endingAvg) AnalyzeData(List<long> values)
    {
        var avg = values.Average();
        var stdDev = Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));

        var startingAvg = values.Take(10).Average();
        var endingAvg = values.TakeLast(10).Average();
        
        return (avg, stdDev, startingAvg, endingAvg);
    }
    
    private (double min, double max) GetValidRange(double average, double stdDev)
    {
        var limit = stdDev * 2;
        return (average - limit, average + limit);
    }

    private double GetTrendLine(List<MemoryUsageBusinessEntity> records)
    {
        var numberOfRecords = records.Count;
        var sumX = records.Sum(x => x.ClientRunTimeSeconds);
        var sumX2 = records.Sum(x => x.ClientRunTimeSeconds * x.ClientRunTimeSeconds);
        var sumY = records.Sum(x => x.MemoryUsageBytes);
        var sumXy = records.Sum(x => x.ClientRunTimeSeconds * x.MemoryUsageBytes);
        
        var slope = (sumXy - ((sumX * sumY) / numberOfRecords )) / (sumX2 - (sumX * sumX / numberOfRecords));

        return slope;
    }
}