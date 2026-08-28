namespace NutriCoach.App.Services;

/// <summary>Kurze, allgemeinverständliche Erklärtexte je Nährstoff ("Über X" / "Wie wirkt es auf mich?"),
/// analog zum Ausklapp-Infofenster in der MCI-App-Vorlage. Rein informativ/allgemein gehalten, keine
/// medizinische Beratung - passend zum Charakter einer Ernährungs-Tracking-App.</summary>
public static class NutrientInfoData
{
    public record Info(string About, string Effect);

    private static readonly Dictionary<string, Info> Data = new()
    {
        ["Zucker"] = new(
            "Zucker sind schnell verfügbare Kohlenhydrate, die von Natur aus in Obst und Milch vorkommen oder Lebensmitteln zugesetzt werden.",
            "Er liefert schnelle Energie, lässt den Blutzucker aber auch schnell wieder abfallen. Zu viel zugesetzter Zucker wird mit Gewichtszunahme in Verbindung gebracht."),
        ["Ballaststoffe"] = new(
            "Ballaststoffe sind unverdauliche Kohlenhydrate aus pflanzlichen Lebensmitteln.",
            "Sie fördern die Verdauung, halten länger satt und unterstützen einen stabilen Blutzuckerspiegel."),
        ["Salz"] = new(
            "Salz (Natriumchlorid) ist lebensnotwendig, steckt aber oft in größeren Mengen in verarbeiteten Lebensmitteln.",
            "In Maßen reguliert es den Flüssigkeitshaushalt; dauerhaft zu viel Salz wird mit erhöhtem Blutdruck in Verbindung gebracht."),
        ["Gesättigte Fettsäuren"] = new(
            "Gesättigte Fettsäuren stecken vor allem in tierischen Produkten wie Butter, Käse und fettem Fleisch.",
            "Sie liefern Energie, gelten aber in großen Mengen als ungünstig für die Blutfettwerte - ungesättigte Fette sind meist die bessere Wahl."),

        ["Vitamin A"] = new(
            "Vitamin A ist fettlöslich und kommt z. B. in Leber, Eiern und als Provitamin (Beta-Carotin) in orangem/grünem Gemüse vor.",
            "Es ist wichtig für das Sehvermögen (besonders bei Dämmerlicht), gesunde Haut und ein funktionierendes Immunsystem."),
        ["Vitamin B1"] = new(
            "Vitamin B1 (Thiamin) steckt u. a. in Vollkornprodukten, Hülsenfrüchten und Schweinefleisch.",
            "Es hilft dem Körper, Kohlenhydrate in Energie umzuwandeln, und unterstützt die Nervenfunktion."),
        ["Vitamin B2"] = new(
            "Vitamin B2 (Riboflavin) findet sich u. a. in Milchprodukten, Eiern und grünem Gemüse.",
            "Es ist an der Energiegewinnung aus der Nahrung beteiligt und trägt zu gesunder Haut und guter Sehkraft bei."),
        ["Vitamin B3"] = new(
            "Vitamin B3 (Niacin) kommt in Fleisch, Fisch, Vollkorn und Nüssen vor.",
            "Es unterstützt den Energiestoffwechsel sowie die Funktion von Nervensystem und Haut."),
        ["Vitamin B5"] = new(
            "Vitamin B5 (Pantothensäure) ist in fast allen Lebensmitteln enthalten, besonders in Fleisch, Vollkorn und Hülsenfrüchten.",
            "Es spielt eine zentrale Rolle bei der Energiegewinnung aus Fetten, Kohlenhydraten und Eiweiß."),
        ["Vitamin B6"] = new(
            "Vitamin B6 steckt u. a. in Geflügel, Fisch, Kartoffeln und Bananen.",
            "Es ist am Eiweißstoffwechsel beteiligt und unterstützt die Bildung roter Blutkörperchen sowie das Nervensystem."),
        ["Vitamin B7"] = new(
            "Vitamin B7 (Biotin) findet sich u. a. in Eiern, Nüssen und Vollkornprodukten.",
            "Es trägt zu normaler Haut, Haaren und einem funktionierenden Energiestoffwechsel bei."),
        ["Vitamin B9"] = new(
            "Vitamin B9 (Folat/Folsäure) kommt vor allem in grünem Blattgemüse, Hülsenfrüchten und Vollkorn vor.",
            "Es ist wichtig für die Zellteilung und Blutbildung - besonders relevant in der Schwangerschaft."),
        ["Vitamin B12"] = new(
            "Vitamin B12 steckt nahezu ausschließlich in tierischen Lebensmitteln wie Fleisch, Fisch, Eiern und Milchprodukten.",
            "Es ist essenziell für die Blutbildung und die Funktion des Nervensystems - für vegan lebende Menschen ist eine Supplementierung meist nötig."),
        ["Vitamin C"] = new(
            "Vitamin C ist reichlich in Zitrusfrüchten, Paprika und vielen anderen Obst- und Gemüsesorten enthalten.",
            "Es unterstützt das Immunsystem, wirkt als Antioxidans und verbessert die Eisenaufnahme aus der Nahrung."),
        ["Vitamin D"] = new(
            "Vitamin D wird größtenteils durch Sonnenlicht in der Haut gebildet, kommt aber auch in fettem Fisch und Eigelb vor.",
            "Es ist entscheidend für den Kalziumhaushalt und damit für gesunde Knochen und Zähne."),
        ["Vitamin E"] = new(
            "Vitamin E ist fettlöslich und steckt vor allem in pflanzlichen Ölen, Nüssen und Samen.",
            "Es wirkt als Antioxidans und schützt Zellen vor oxidativem Stress."),
        ["Vitamin K"] = new(
            "Vitamin K kommt vor allem in grünem Blattgemüse wie Grünkohl und Spinat vor.",
            "Es ist notwendig für eine normale Blutgerinnung und trägt zur Knochengesundheit bei."),

        ["Kalzium"] = new(
            "Kalzium ist in Milchprodukten, grünem Gemüse und angereicherten pflanzlichen Alternativen enthalten.",
            "Es ist der Hauptbaustein von Knochen und Zähnen und wichtig für Muskel- und Nervenfunktion."),
        ["Magnesium"] = new(
            "Magnesium steckt u. a. in Nüssen, Vollkorn, Hülsenfrüchten und grünem Gemüse.",
            "Es ist an über 300 Stoffwechselprozessen beteiligt und wichtig für Muskel- und Nervenfunktion sowie den Energiestoffwechsel."),
        ["Kalium"] = new(
            "Kalium ist reichlich in Obst, Gemüse, Kartoffeln und Hülsenfrüchten enthalten.",
            "Es reguliert gemeinsam mit Natrium den Flüssigkeitshaushalt und ist wichtig für Muskel- und Herzfunktion."),
        ["Natrium"] = new(
            "Natrium ist Teil des Kochsalzes und in vielen verarbeiteten Lebensmitteln enthalten.",
            "Es reguliert den Flüssigkeitshaushalt - ein dauerhafter Überschuss wird mit erhöhtem Blutdruck in Verbindung gebracht."),
        ["Phosphor"] = new(
            "Phosphor steckt in nahezu allen proteinreichen Lebensmitteln wie Fleisch, Fisch und Milchprodukten.",
            "Er ist gemeinsam mit Kalzium wichtig für Knochen und Zähne und Teil der körpereigenen Energiewährung ATP."),
        ["Chlorid"] = new(
            "Chlorid ist der zweite Bestandteil von Kochsalz und kommt entsprechend in gesalzenen Lebensmitteln vor.",
            "Es ist Bestandteil der Magensäure und hilft, den Flüssigkeits- und Säure-Basen-Haushalt zu regulieren."),
        ["Schwefel"] = new(
            "Schwefel ist Bestandteil bestimmter Aminosäuren und kommt in proteinreichen Lebensmitteln wie Fleisch, Fisch und Eiern vor.",
            "Er ist ein Baustein von Proteinen und trägt zur Bildung von Haut, Haaren und Bindegewebe bei."),
        ["Eisen"] = new(
            "Eisen kommt in rotem Fleisch, Hülsenfrüchten und grünem Blattgemüse vor (aus Fleisch wird es besser aufgenommen).",
            "Es ist Bestandteil des roten Blutfarbstoffs und damit essenziell für den Sauerstofftransport im Blut."),
        ["Zink"] = new(
            "Zink steckt u. a. in Fleisch, Meeresfrüchten, Hülsenfrüchten und Nüssen.",
            "Es unterstützt das Immunsystem, die Wundheilung und zahlreiche Stoffwechselprozesse."),
        ["Selen"] = new(
            "Selen findet sich in Paranüssen, Fisch und Fleisch, abhängig vom Selengehalt des Bodens.",
            "Es wirkt als Antioxidans und ist wichtig für die Schilddrüsenfunktion."),
        ["Kupfer"] = new(
            "Kupfer kommt in Nüssen, Vollkorn, Hülsenfrüchten und Innereien vor.",
            "Es ist an der Blutbildung, dem Eisenstoffwechsel und der Bindegewebsbildung beteiligt."),
        ["Mangan"] = new(
            "Mangan steckt vor allem in Vollkornprodukten, Nüssen und Tee.",
            "Es unterstützt den Knochenaufbau und wirkt als Bestandteil bestimmter Enzyme antioxidativ."),
        ["Jod"] = new(
            "Jod ist vor allem in Meeresfisch, Milchprodukten und jodiertem Speisesalz enthalten.",
            "Es ist ein essenzieller Baustein der Schilddrüsenhormone, die den gesamten Stoffwechsel steuern."),
        ["Fluorid"] = new(
            "Fluorid kommt in geringen Mengen in Trinkwasser, Tee und fluoridiertem Salz/Zahnpasta vor.",
            "Es stärkt den Zahnschmelz und trägt zum Schutz vor Karies bei."),
        ["Chrom"] = new(
            "Chrom steckt in kleinen Mengen in Vollkornprodukten, Brokkoli und Fleisch.",
            "Es spielt vermutlich eine Rolle im Kohlenhydratstoffwechsel und bei der Wirkung von Insulin."),
        ["Molybdän"] = new(
            "Molybdän kommt in Hülsenfrüchten, Getreide und Nüssen vor.",
            "Es ist Bestandteil mehrerer Enzyme, u. a. für den Abbau von Aminosäuren."),
        ["Kobalt"] = new(
            "Kobalt wird über die Nahrung fast ausschließlich als Bestandteil von Vitamin B12 aufgenommen.",
            "Es ist der zentrale Baustein im Vitamin-B12-Molekül und damit indirekt an der Blutbildung beteiligt."),
        ["Silizium"] = new(
            "Silizium findet sich u. a. in Vollkorngetreide, Bananen und grünen Bohnen.",
            "Es wird mit der Gesundheit von Bindegewebe, Haut und Knochen in Verbindung gebracht, ist aber wissenschaftlich noch nicht abschließend als essenziell eingestuft."),

        ["Zuckeralkohole"] = new(
            "Zuckeralkohole (z. B. Xylit, Erythrit) sind Zuckerersatzstoffe, die oft in zuckerfreien Produkten stecken.",
            "Sie liefern weniger oder keine Kalorien und beeinflussen den Blutzucker kaum, können in großen Mengen aber abführend wirken."),
        ["Alkohol"] = new(
            "Alkohol entsteht durch Gärung und liefert mit 7 kcal pro Gramm fast so viel Energie wie Fett.",
            "Er liefert leere Kalorien ohne Nährwert und kann in größeren Mengen die Regeneration und den Stoffwechsel beeinträchtigen."),
        ["Omega-3-Fettsäuren"] = new(
            "Omega-3-Fettsäuren sind mehrfach ungesättigte Fette, reich enthalten in fettem Fisch, Leinsamen und Walnüssen.",
            "Sie werden mit einer gesunden Herz-Kreislauf-Funktion und entzündungshemmenden Effekten in Verbindung gebracht."),
        ["Omega-6-Fettsäuren"] = new(
            "Omega-6-Fettsäuren stecken vor allem in pflanzlichen Ölen wie Sonnenblumen- oder Distelöl.",
            "Sie sind essenziell, in der westlichen Ernährung aber meist reichlich vorhanden - ein ausgewogenes Verhältnis zu Omega-3 gilt als günstig."),
        ["Omega-9-Fettsäuren"] = new(
            "Omega-9-Fettsäuren sind einfach ungesättigte Fette, allen voran Ölsäure in Olivenöl und Avocados.",
            "Sie gelten als vorteilhaft für die Blutfettwerte, sind aber im Gegensatz zu Omega-3/6 nicht essenziell, da der Körper sie selbst bilden kann."),
    };

    public static Info Get(string name) => Data.TryGetValue(name, out var info)
        ? info
        : new Info("Für diesen Nährstoff liegt noch keine Beschreibung vor.", "Für diesen Nährstoff liegt noch keine Beschreibung vor.");
}
