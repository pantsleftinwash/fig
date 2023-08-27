using Fig.Client;
using Fig.Client.Attributes;
using Fig.Client.Validation;
using Fig.Contracts.SettingVerification;

namespace Fig.Examples.ConsoleApp;

[Verification("Check Website", "$Fig.Examples.ConsoleApp.ConsoleApp.md#CheckWebsite", typeof(CheckWebsiteVerification), TargetRuntime.Dotnet6)]
public class ConsoleSettings : SettingsBase, IConsoleSettings
{
    public override string ClientName => "Console App #66";
    public override string ClientDescription => "Basic console app for testing and showcasing new features.";

    [Setting("$Fig.Examples.ConsoleApp.ConsoleApp.md#UseService", false, false)]
    [EnablesSettings(nameof(ServiceUsername), nameof(ServicePassword))]
    [DisplayOrder(1)]
    public bool UseService { get; set; }
    
    [Setting("the username")]
    [DisplayOrder(2)]
    [Validation(ValidationType.NotEmpty)]
    public string? ServiceUsername { get; set; }
    
    [Setting("the password")]
    [Secret]
    [DisplayOrder(3)]
    public string? ServicePassword { get; set; }
    
    [Setting("some other setting", 1)]
    public int UnrelatedSetting { get; set; }
    
    [Setting("My Animals", defaultValueMethodName: "GetAnimals")]
    public List<Animal> MyAnimals { get; set; }

    public static List<Animal> GetAnimals()
    {
        return new List<Animal>()
        {
            new Animal
            {
                Name = "l",
                Legs = 4,
                FavouriteFood = "m"
            }
        };
    }

    public class Animal
    {
        [Validation("[0-9a-zA-Z]{5,}", "Must have 5 or more characters and a much longer thing that will test if this thing wraps or if it just goes out of scope. We will see I guess.")]
        public string Name { get; set; }
        
        public int Legs { get; set; }
        
        [Validation(ValidationType.NotEmpty)]
        public string FavouriteFood { get; set; }
    }


    

    // [Setting("A data grid setting")]
    // public List<MyClass> DataGridSetting { get; set; }
    //
    // [Setting("The address of the website", "http://www.google.com")]
    // public string WebsiteAddress { get; set; }
    //
    // [Setting("My favourite animal", "Cow")]
    // [Secret]
    // public string FavouriteAnimal { get; set; }
    //
    // [Setting("My favourite number", 66)]
    // public int FavouriteNumber { get; set; }
    //
    // [Setting("True or false, your choice...", false)]
    // public bool TrueOrFalse { get; set; }

    /*[Setting("Enum value", "Cat")]
    [ValidValues(typeof(Animals))]
    public string? Pets { get; set; }

    [Setting("Enum value", "Shark")]
    //[ValidValues("Shark", "Whale", "Salmon")]
    [Secret]
    public string? Fish { get; set; }

    [Setting("Enum value", 1)]
    [LookupTable("Animals2")]
    public int AustralianAnimals { get; set; }

    [Setting("Enum value", 1)]
    [ValidValues("1 -> Moose", "2 -> Tick", "4 -> Deer")]
    public int SwedishAnimals { get; set; }*/
}

public class MyClass
{
    public string Name { get; set; }
    
    [MultiLine(4)]
    public string Value { get; set; }
}

public enum Animals
{
    Cat,
    Dog,
    Horse
}