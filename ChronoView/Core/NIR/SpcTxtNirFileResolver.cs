using System.IO;

namespace ChronoView.Core.NIR;

/// <summary>
/// NIR file resolver for .spc/.txt file sets with 'A' suffix pattern.
/// Format: run_XXXXX.spc + run_XXXXXА.txt
/// </summary>
public class SpcTxtNirFileResolver : INirFileResolver
{
    public string GetNirKey(string nirFilePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(nirFilePath);
        
        // Remove 'A' suffix if present (from .txt files)
        if (fileName.EndsWith("A", StringComparison.Ordinal))
        {
            return fileName.Substring(0, fileName.Length - 1);
        }
        
        return fileName;
    }
    
    public string GetPrimaryFilePath(string nirKey, string nirDirectory)
    {
        // Primary file is .spc
        return Path.Combine(nirDirectory, nirKey + ".spc");
    }
    
    public string? GetTextFilePath(string nirKey, string nirDirectory)
    {
        // Try A.txt suffix first (priority)
        var txtPathA = Path.Combine(nirDirectory, nirKey + "A.txt");
        if (File.Exists(txtPathA))
            return txtPathA;
        
        // Fallback to plain .txt
        var txtPath = Path.Combine(nirDirectory, nirKey + ".txt");
        if (File.Exists(txtPath))
            return txtPath;
        
        return null;
    }
    
    public IEnumerable<string> GetAllFilePaths(string nirKey, string nirDirectory)
    {
        // .spc file
        var spcPath = GetPrimaryFilePath(nirKey, nirDirectory);
        if (File.Exists(spcPath))
            yield return spcPath;
        
        // .txt file (with A suffix or plain)
        var txtPath = GetTextFilePath(nirKey, nirDirectory);
        if (txtPath != null)
            yield return txtPath;
    }
    
    public string GetScanPattern()
    {
        // Scan .txt files only (to avoid duplicate groups from .spc)
        return "*.txt";
    }
}
