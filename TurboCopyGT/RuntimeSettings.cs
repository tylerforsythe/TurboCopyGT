namespace TurboCopyGT;

/// <summary>
/// Settings that we keep for just this session -- not persisted to disk.
/// CLI option is passed to the main program and stored here for use throughout the program.
/// </summary>
public static class RuntimeSettings
{
    /// <summary>
    /// Whether to shuffle the data set before handing it off to threads.
    /// </summary>
    public static bool UseShuffle { get; set; } = true;
    
    /// <summary>
    /// Convenience method to turn a bool value into a yes or no string.
    /// </summary>
    public static string BoolToYesNo(bool input) => input ? "yes" : "no";
}