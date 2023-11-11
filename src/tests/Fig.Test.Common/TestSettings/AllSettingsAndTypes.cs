using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class AllSettingsAndTypes : TestSettingsBase
{
    public override string ClientName => "AllSettingsAndTypes";
    public override string ClientDescription => "Sample settings with all types of settings";


    [Setting("String Setting")]
    public string StringSetting { get; set; } = "Cat";

    [Setting("Int Setting")] 
    public int IntSetting { get; set; } = 34;

    [Setting("Long Setting")] 
    public long LongSetting { get; set; } = 64;

    [Setting("Long Setting")] 
    public double DoubleSetting { get; set; } = 45.3;

    [Setting("Date Time Setting")]
    public DateTime? DateTimeSetting { get; set; }

    [Setting("Time Span Setting")]
    public TimeSpan? TimespanSetting { get; set; }

    [Setting("Bool Setting")] 
    public bool BoolSetting { get; set; } = true;

    [Setting("Common LookupTable Setting")]
    [LookupTable("States")]
    public long LookupTableSetting { get; set; } = 5;

    [Setting("Secret Setting")]
    [Secret]
    public string SecretSetting { get; set; } = "SecretString";

    [Setting("String Collection")]
    public List<string>? StringCollectionSetting { get; set; }

    [Setting("Object List Setting")]
    public List<SomeSetting>? ObjectListSetting { get; set; }

    [Setting("Enum Setting")]
    [ValidValues(typeof(Pets))]
    public Pets EnumSetting { get; set; } = Pets.Cat;

    [Setting("Json Setting")]
    public SomeSetting? JsonSetting { get; set; }

    public override void Validate(ILogger logger)
    {
        //Perform validation here.
        SetConfigurationErrorStatus(false);
    }
}

public class SomeSetting
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = null!;

    public int MyInt { get; set; }

    public override bool Equals(object? obj)
    {
        var other = obj as SomeSetting;
        return $"{Key}-{Value}-{MyInt}" == $"{other?.Key}-{other?.Value}-{other?.MyInt}";
    }
}

public enum Pets
{
    Cat,
    Dog,
    Fish
}