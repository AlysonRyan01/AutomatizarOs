namespace AutomatizarOs.Core;

public static class Configuration
{
    public static string ConnectionString { get; set; } = string.Empty;
    public static string BackendUrl { get; set; } = "http://localhost:5020";
    public static string FrontendUrl { get; set; } = "http://localhost:5075";

}