﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using CopyFromGenerator;

namespace ManualTest;

// Source data classes
public partial class Customer
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Address { get; set; } = "";

    [GenerateCopyFromMethod]
    public partial void CopyFrom(Employee employee);

    [GenerateCopyFromMethod]
    public partial void CopyFrom(Person person);

}

// Target classes demonstrating both generators
[GenerateCopyFromMethod]
public partial class Person
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Address { get; set; } = "";
}

[GenerateCopyFromMethod]
public partial class Employee : Person
{
    public string Department { get; set; } = "";
}

public static partial class Transformer
{
    [GenerateCopyFromMethod]
    public static partial void CopyFrom(Employee employee, Customer customer);
}


public class Program
{
    public static void Main()
    {
        // Test CopyFromMethod generator (same class copying)
        var employee1 = new Employee { Name = "John", Age = 30, Address = "USA", Department = "IT" };
        Employee employee2 = new Employee();
        employee2.CopyFrom(employee1);
        Console.WriteLine($"Employee copy - Name: {employee2.Name}, Age: {employee2.Age}, Address: {employee2.Address}, Department: {employee2.Department}");
        
        // Test CopyFromOtherMethod generator (copying from other types)
        var customer = new Customer();
        customer.CopyFrom(employee2);
        Console.WriteLine($"Customer from Employee - Name: {customer.Name}, Age: {customer.Age}"); 

        // Test static method
        Employee employee3 = new Employee();
        Transformer.CopyFrom(employee3, customer);
        Console.WriteLine($"Employee from Employee - Name: {employee3.Name}, Age: {employee3.Age}"); 

    }
}
