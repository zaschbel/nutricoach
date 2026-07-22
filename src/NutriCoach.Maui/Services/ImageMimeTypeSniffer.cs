namespace NutriCoach.Maui.Services;

/// <summary>
/// Erkennt den tatsächlichen Bildtyp (PNG/JPEG/HEIC) anhand der Magic Bytes der Bilddaten selbst,
/// statt sich auf photo.ContentType zu verlassen - das ist bei frisch aufgenommenen Kamerafotos auf
/// iOS oft leer/unzuverlässig (im Gegensatz zu Galerie-Auswahl). Ursprünglich nur in AddFoodPage
/// für die Essens-Fotoerkennung genutzt, jetzt als gemeinsame Hilfsklasse ausgelagert, weil das
/// Zukunftsbild-Feature (Statistiken-Reiter) denselben Foto-Upload-Flow für Gemini braucht.
/// </summary>
public static class ImageMimeTypeSniffer
{
    public static string? DetectImageMimeType(byte[] bytes)
    {
        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            return "image/png";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return "image/jpeg";
        if (bytes.Length >= 12 && bytes[8] == 0x48 && bytes[9] == 0x45 && bytes[10] == 0x49 && bytes[11] == 0x43)
            return "image/heic";
        return null;
    }
}
