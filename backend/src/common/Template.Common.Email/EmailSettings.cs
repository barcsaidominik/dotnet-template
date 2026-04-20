using System.ComponentModel.DataAnnotations;

namespace Template.Common.Email;

public sealed class EmailSettings
{
    public const string SECTION_NAME = "Email";

    public string SmtpHost
    {
        get;
        set;
    } = string.Empty;

    [Range(1, 65535)]
    public int SmtpPort
    {
        get;
        set;
    } = 587;

    [EmailAddress]
    public string From
    {
        get;
        set;
    } = string.Empty;

    public string Username
    {
        get;
        set;
    } = string.Empty;

    public string Password
    {
        get;
        set;
    } = string.Empty;
}
