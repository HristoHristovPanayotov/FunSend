namespace FunSend.Data.Seeding;

/// <summary>
/// Settings for the initial data. Bound from the "Seed" configuration section
/// so that the demo password is never compiled into the assembly.
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// When false only roles and categories are created. Turn the demo
    /// accounts off for any deployment that is not the project demo.
    /// </summary>
    public bool CreateDemoAccounts { get; set; } = true;

    /// <summary>
    /// Password given to the three demo accounts. Supplied by configuration
    /// (appsettings, user secrets or an environment variable).
    /// </summary>
    public string DemoPassword { get; set; } = string.Empty;
}
