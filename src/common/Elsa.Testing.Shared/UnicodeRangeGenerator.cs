namespace Elsa.Testing.Shared;

/// <summary>
/// Generates Unicode ranges.
/// </summary>
public static class UnicodeRangeGenerator
{
    /// <summary>
    /// Generates a range of Unicode characters.
    /// </summary>
    public static string GenerateUnicodeString()
    {
        // This method generates a string containing characters from various Unicode ranges.
        var sb = new System.Text.StringBuilder();
        
        // Add some characters from various ranges
        sb.Append('a'); // Basic Latin
        sb.Append('\u0370'); // Greek and Coptic
        sb.Append('\u200B'); // Zero width space
        sb.Append('\u4E00'); // CJK Unified Ideographs

        // German umlauts (in unicode):
        sb.Append('\u00E4'); // ä
        sb.Append('\u00F6'); // ö
        sb.Append('\u00FC'); // ü
        sb.Append('\u00DF'); // ß
        
        // French characters (in unicode):
        sb.Append('\u00E0'); // à
        sb.Append('\u00E2'); // â
        sb.Append('\u00E7'); // ç
        sb.Append('\u00E8'); // è

        // Chinese characters (in unicode):
        sb.Append('\u4E00'); // 一
        sb.Append('\u4E01'); // 丁
        sb.Append('\u4E02'); // 丂
        sb.Append('\u4E03'); // 七

        // Russian characters (in unicode):
        sb.Append('\u0410'); // А
        sb.Append('\u0411'); // Б
        sb.Append('\u0412'); // В
        sb.Append('\u0413'); // Г
        sb.Append('\u0414'); // Д
        sb.Append('\u0415'); // Е

        // Arabic characters (in unicode):
        sb.Append('\u0627'); // ا
        sb.Append('\u0628'); // ب
        sb.Append('\u0629'); // ة
        sb.Append('\u062A'); // ت

        // Syriac characters (in unicode):
        sb.Append('\u0710'); // ܐ
        sb.Append('\u0711'); // ܑ
        sb.Append('\u0712'); // ܒ
        sb.Append('\u0713'); // ܓ
        sb.Append('\u0714'); // ܔ
        
        // Hebrew characters (in unicode):
        sb.Append('\u05D0'); // א
        sb.Append('\u05D1'); // ב
        sb.Append('\u05D2'); // ג
        sb.Append('\u05D3'); // ד

        return sb.ToString();
    }
    
}