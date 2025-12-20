using p3rpc.nativetypes.Interfaces;

#pragma warning disable CS1591

namespace p3rpc.commonmodutils
{
    public class ConfigColor(byte r, byte g, byte b, byte a)
    {
        // Basic Color Presets
        public static readonly ConfigColor White = new(0xff, 0xff, 0xff, 0xff);
        public static readonly ConfigColor Black = new(0x0,  0x0,  0x0,  0xff);
        public static readonly ConfigColor Red =   new(0xff, 0x0,  0x0,  0xff);
        public static readonly ConfigColor Green = new(0x0,  0xff, 0x0,  0xff);
        public static readonly ConfigColor Blue =  new(0x0,  0x0,  0xff, 0xff);

        // Color Presets - For Femc Reloaded Project
        // https://github.com/MadMax1960/Femc-Reloaded-Project
        public static readonly ConfigColor LightColor = new(0xff, 0xbf, 0xfc, 0xff);
        public static readonly ConfigColor DarkColor = new(0xd4, 0x15, 0x5b, 0xff);
        public static readonly ConfigColor MidColor = new(0xff, 0x8f, 0xec, 0xff);

        public static readonly ConfigColor CampBgColor = new(0xe8, 0x64, 0xbc, 0xff);
        public static readonly ConfigColor TextBoxBackFillColor = new(0x6c, 0x7, 0x39, 0xff);
        public static readonly ConfigColor TextBoxFrontFillColor = new(0x49, 0x4, 0x21, 0xff);
        public static readonly ConfigColor CampMenuItemColor1 = new(0xff, 0x8f, 0xec, 0xff);
        public static readonly ConfigColor CampMenuItemColor2 = new(0xf7, 0x83, 0xe4, 0xff);
        public static readonly ConfigColor CampMenuItemColor3 = new(0xe0, 0x79, 0xcf, 0xff);
        public static readonly ConfigColor CampMenuItemColorNoSel = new(0xcf, 0x7c, 0xc1, 0xff);
        public static readonly ConfigColor CheckFgBorder = new(0x9b, 0x0b, 0x47, 0xff);
        public static readonly ConfigColor SocialStatsCourage = new(0xf5, 0x62, 0xa7, 0xff);
        public static readonly ConfigColor SocialStatsCharm = new(0xff, 0x8f, 0xec, 0xff); // same as mid color
        public static readonly ConfigColor SocialStatsAcademics = new(0xa0, 0x0c, 0x42, 0xff);
        public static readonly ConfigColor MailIconInnerColor = new(0xff, 0x7f, 0x9f, 0xff);
        public static readonly ConfigColor DateTimeWaterColor = new(0xf0, 0x7c, 0xcd, 0xff);
        public static readonly ConfigColor TextBoxLeftHaze = new(0x83, 0x06, 0x51, 0xff);
        public static readonly ConfigColor TextBoxSpeakerNameTriangle = new(0xc8, 0x05, 0x4b, 0xff);
        // I'd put the discord attachment here but they're time limited now
        public static readonly ConfigColor MellodiColorLight1 = new(0xff, 0xbd, 0xce, 0xff);
        public static readonly ConfigColor MellodiColorLight2 = new(0xfe, 0x9d, 0xb6, 0xff);
        public static readonly ConfigColor MellodiColorLight3 = new(0xff, 0x89, 0xa6, 0xff);
        public static readonly ConfigColor MellodiColorMid1 = new(0xd4, 0x15, 0x5b, 0xff);
        public static readonly ConfigColor MellodiColorMid2 = new(0xff, 0x4a, 0x77, 0xff);
        public static readonly ConfigColor MellodiColorMid3 = new(0xcd, 0x62, 0x90, 0xff);
        public static readonly ConfigColor MellodiColorMid4 = new(0xd4, 0x45, 0x92, 0xff);
        public static readonly ConfigColor MellodiColorDark1 = new(0xb6, 0x3f, 0x67, 0xff);
        public static readonly ConfigColor MellodiColorDark2 = new(0x81, 0x0, 0x6, 0xff);
        public static readonly ConfigColor MellodiColorDark3 = new(0x49, 0x4, 0x21, 0xff);

        public static readonly ConfigColor BackLogBlackBoard = new(0x09, 0x03, 0x09, 0xff);
        public static readonly ConfigColor DayChangeMoonShadow = new(0xa3, 0x26, 0x50, 0xff);
        public static readonly ConfigColor ShopFillColor = new(0xaf, 0xf, 0x6e, 0xff);
        public static readonly ConfigColor ShopShadowColor = new(0xff, 0x4e, 0xdc, 0xff);
        public static readonly ConfigColor GetItemFillMask = new(0x54, 0xd, 0x54, 0xff);
        public static readonly ConfigColor GetItemGotTextColor = new(0xff, 0x4a, 0xff, 0xff);
        public static readonly ConfigColor MindSelectDotColor = new(0x67, 0x00, 0x00, 0xff);
        public static readonly ConfigColor MsgWindowSystemDark = new(0x2f, 0x00, 0x14, 0xff);

        public static readonly ConfigColor PersonaStatusSkillListBg = new(0x2a, 0x00, 0x12, 0xff);
        public static readonly ConfigColor PersonaStatusSkillListCheckboardAlt = new(0x5c, 0x27, 0x3e, 0xff);
        
        public byte R { get; set; } = r;
        public byte G { get; set; } = g;
        public byte B { get; set; } = b;
        public byte A { get; set; } = a;

        public uint ToU32() => (uint)(R << 0x18) | (uint)(G << 0x10) | (uint)(B << 0x8) | A;
        public uint ToU32IgnoreAlpha() => (uint)(R << 0x18) | (uint)(G << 0x10) | (uint)(B << 0x8);
        public uint ToU32ARGB() => (uint)(A << 0x18) | (uint)(R << 0x10) | (uint)(G << 0x8) | B;

        public static FColor ToFColor(ConfigColor cfgColor) => new FColor(cfgColor.R, cfgColor.G, cfgColor.B, cfgColor.A);
        public static FColor ToFColorWithAlpha(ConfigColor cfgColor, byte alpha) => new FColor(cfgColor.R, cfgColor.G, cfgColor.B, alpha);
        public static FSprColor ToFSprColor(ConfigColor cfgColor) => new FSprColor(cfgColor.R, cfgColor.G, cfgColor.B, cfgColor.A);
        public static FSprColor ToFSprColorWithAlpha(ConfigColor cfgColor, byte alpha) => new FSprColor(cfgColor.R, cfgColor.G, cfgColor.B, alpha);
        public static FLinearColor ToFLinearColor(ConfigColor cfgColor) => new FLinearColor((float)cfgColor.R / 256, (float)cfgColor.G / 256, (float)cfgColor.B / 256, (float)cfgColor.A / 256);
        public static FColor ToFColorBP(ConfigColor cfgColor) => new FColor(cfgColor.A, cfgColor.R, cfgColor.G, cfgColor.B);
        public static FColor ToFColorBPWithAlpha(ConfigColor cfgColor, byte alpha) => new FColor(alpha, cfgColor.R, cfgColor.G, cfgColor.B);
        public static void SetColor(ref FSprColor color, ConfigColor cfgColor)
        {
            color.R = cfgColor.R;
            color.G = cfgColor.G;
            color.B = cfgColor.B;
            color.A = cfgColor.A;
        }

        public static void SetColorIgnoreAlpha(ref FSprColor color, ConfigColor cfgColor)
        {
            color.R = cfgColor.R;
            color.G = cfgColor.G;
            color.B = cfgColor.B;
        }

        public static void SetColorCustomAlpha(ref FSprColor color, ConfigColor cfgColor, byte alpha)
        {
            color.R = cfgColor.R;
            color.G = cfgColor.G;
            color.B = cfgColor.B;
            color.A = alpha;
        }

        public static void SetColor(ref FColor color, ConfigColor cfgColor)
        {
            color.R = cfgColor.R;
            color.G = cfgColor.G;
            color.B = cfgColor.B;
            color.A = cfgColor.A;
        }

        public static void SetColorIgnoreAlpha(ref FColor color, ConfigColor cfgColor)
        {
            color.R = cfgColor.R;
            color.G = cfgColor.G;
            color.B = cfgColor.B;
        }

        public static void SetColorCustomAlpha(ref FColor color, ConfigColor cfgColor, byte alpha)
        {
            color.R = cfgColor.R;
            color.G = cfgColor.G;
            color.B = cfgColor.B;
            color.A = alpha;
        }

        public static void SetColor(ref FLinearColor color, ConfigColor cfgColor)
        {
            color.R = (float)cfgColor.R / 256;
            color.G = (float)cfgColor.G / 256;
            color.B = (float)cfgColor.B / 256;
            color.A = (float)cfgColor.A / 256;
        }
    }
}
