namespace AgriStream.Models;

public static class Roles
{
    public const string Farmer = "Farmer";
    public const string Agronomist = "Agronomist";
    public const string Admin = "Admin";

    public static readonly string[] All = [Farmer, Agronomist, Admin];
}
