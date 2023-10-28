using System.Security.Cryptography;
using System.Text;
using Colourful;

namespace Cimon.Shared;

public class GradientGenerator
{
    private static readonly RGBColor[] PastelColors = new[] {
        "#FFB3BA", "#FFDFBA", "#BAFFC9", "#BAE1FF", "#FFB0A0",
        "#FFEBB7", "#CFFFB3", "#B0E0FF", "#FFA1B5", "#FFF1B5",
        "#FF90A1", "#FFC1A0", "#E1FFA9", "#A9D0FF", "#FF8090",
        "#FFA080", "#B5FFB0", "#8090FF", "#FF7080", "#FF9070",
        "#90FF80", "#7080FF", "#FF606F", "#FF8060", "#70FF90",
        "#606FFF", "#FF507F", "#FF7050", "#50FF70", "#5050FF",
        "#FFD0A0", "#D0FFB0", "#70A0FF", "#FFC090", "#B0FFD0",
        "#90B0FF", "#FFB070", "#A0FFC0", "#8080FF", "#FFA050",
        "#90E0FF", "#FF9040", "#70D0FF", "#FF8040", "#60B0FF",
        "#FF7020", "#60A0FF", "#FF6010", "#5090FF", "#FF5010"
    }.Select(HexToRgb).ToArray();

    private byte[] ComputeSha256Hash(string rawData) {
        using SHA256 sha256Hash = SHA256.Create();
        return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
    }

    private static RGBColor HexToRgb(string hex) {
        return new RGBColor(Convert.ToInt32(hex.Substring(1, 2), 16),
            Convert.ToInt32(hex.Substring(3, 2), 16),
            Convert.ToInt32(hex.Substring(5, 2), 16));
    }

    private bool AreColorsContrasting(RGBColor color1, RGBColor color2, int threshold, int lThreshold) {
        var converter = new ConverterBuilder()
            .FromRGB()
            .ToLab()
            .Build();

        var lab1 = converter.Convert(color1);
        var lab2 = converter.Convert(color2);

        var deltaE = new CIE76ColorDifference().ComputeDifference(lab1, lab2);
        return deltaE > threshold && Math.Abs(lab1.L - lab2.L) > lThreshold;
    }

    private string ToHexString(RGBColor color) {
        int r = (int)color.R;
        int g = (int)color.G;
        int b = (int)color.B;
        return $"#{r:x2}{g:x2}{b:x2}";
    }

    public (string color1, string color2) GetContrastingColors(string text) {
        var hash = ComputeSha256Hash(text);
        //hash = Guid.NewGuid().ToByteArray();
        int index1 = hash[0] % PastelColors.Length;
        int index2 = hash[1] % PastelColors.Length;

        RGBColor color1 = PastelColors[index1];
        RGBColor color2 = PastelColors[index2];

        var hashIndex = 1; 
        while (hashIndex < hash.Length &&
               !AreColorsContrasting(color1, color2, 1000, 40)) {
            hashIndex++;
            index2 = hash[hashIndex] % PastelColors.Length;
            color2 = PastelColors[index2];
        }
        return (ToHexString(color1), ToHexString(color2));
    }
}