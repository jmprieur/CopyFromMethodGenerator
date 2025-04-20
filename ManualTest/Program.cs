namespace ManualTest;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateCopyFromMethodAttribute : Attribute
{
}

[GenerateCopyFromMethod]
public partial class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Address { get; set; } = "";
}

public class Program
{
    public static void Main()
    {
        var person1 = new Person { Name = "John", Age = 30, Address = "USA" };
        var person2 = new Person();
        
        person2.CopyFrom(person1);
        
        Console.WriteLine($"Person 2 - Name: {person2.Name}, Age: {person2.Age}, Age: {person2.Address}");
        // Should output: Person 2 - Name: John, Age: 30
    }
}
