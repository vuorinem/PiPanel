namespace PiPanel.Device.Display;

public static class Segments
{
    public static byte Empty = 0b_1111_1111;
    public static byte Full = 0b_0000_0000;
    public static byte Top = 0b_1111_1110;
    public static byte RightTop = 0b_1111_1101;
    public static byte RightBottom = 0b_1111_1011;
    public static byte Bottom = 0b_1111_0111;
    public static byte LeftBottom = 0b_1110_1111;
    public static byte LeftTop = 0b_1101_1111;
    public static byte Center = 0b_1011_1111;
    public static byte Dot = 0b_0111_1111;

    public static byte GetForCharacter(char character) => character switch
    {
        ' ' => Empty,
        '0' => (byte)(Top & RightTop & RightBottom & Bottom & LeftBottom & LeftTop),
        '1' => (byte)(RightTop & RightBottom),
        '2' => (byte)(Top & RightTop & Center & LeftBottom & Bottom),
        '3' => (byte)(Top & RightTop & RightBottom & Bottom & Center),
        '4' => (byte)(RightTop & RightBottom & LeftTop & Center),
        '5' => (byte)(Top & RightBottom & Bottom & LeftTop & Center),
        '6' => (byte)(Top & RightBottom & Bottom & LeftBottom & LeftTop & Center),
        '7' => (byte)(Top & RightTop & RightBottom),
        '8' => (byte)(Top & RightTop & RightBottom & Bottom & LeftBottom & LeftTop & Center),
        '9' => (byte)(Top & RightTop & RightBottom & Bottom & LeftTop & Center),
        'C' => (byte)(Top & Bottom & LeftBottom & LeftTop),
        'H' => (byte)(RightTop & RightBottom & LeftBottom & LeftTop & Center),
        '-' => Center,
        _ => Full,
    };

    public static byte[] GetDisplaysForText(string text, int numberOfDisplays)
    {
        byte[] segmentBytes = new byte[numberOfDisplays];

        Array.Fill(segmentBytes, Empty);

        var byteIndex = 0;

        foreach (var character in text)
        {
            if (character == '.')
            {
                // Set dot-flag from previous decimal to light the led
                segmentBytes[byteIndex - 1] &= Dot;
            }
            else
            {
                segmentBytes[byteIndex] = GetForCharacter(character);
                byteIndex++;
            }
        }

        return segmentBytes;
    }
}