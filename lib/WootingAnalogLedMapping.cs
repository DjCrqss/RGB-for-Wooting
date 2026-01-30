using RGB.NET.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WootingRGB.lib
{
    internal static class WootingAnalogLedMapping
    {
        internal static Dictionary<LedId, ushort> HidCodes { get; } = new()
        {
            [LedId.Keyboard_A] = 4,
            [LedId.Keyboard_B] = 5,
            [LedId.Keyboard_C] = 6,
            [LedId.Keyboard_D] = 7,
            [LedId.Keyboard_E] = 8,
            [LedId.Keyboard_F] = 9,
            [LedId.Keyboard_G] = 10,
            [LedId.Keyboard_H] = 11,
            [LedId.Keyboard_I] = 12,
            [LedId.Keyboard_J] = 13,
            [LedId.Keyboard_K] = 14,
            [LedId.Keyboard_L] = 15,
            [LedId.Keyboard_M] = 16,
            [LedId.Keyboard_N] = 17,
            [LedId.Keyboard_O] = 18,
            [LedId.Keyboard_P] = 19,
            [LedId.Keyboard_Q] = 20,
            [LedId.Keyboard_R] = 21,
            [LedId.Keyboard_S] = 22,
            [LedId.Keyboard_T] = 23,
            [LedId.Keyboard_U] = 24,
            [LedId.Keyboard_V] = 25,
            [LedId.Keyboard_W] = 26,
            [LedId.Keyboard_X] = 27,
            [LedId.Keyboard_Y] = 28,
            [LedId.Keyboard_Z] = 29,

            [LedId.Keyboard_1] = 30,
            [LedId.Keyboard_2] = 31,
            [LedId.Keyboard_3] = 32,
            [LedId.Keyboard_4] = 33,
            [LedId.Keyboard_5] = 34,
            [LedId.Keyboard_6] = 35,
            [LedId.Keyboard_7] = 36,
            [LedId.Keyboard_8] = 37,
            [LedId.Keyboard_9] = 38,
            [LedId.Keyboard_0] = 39,

            [LedId.Keyboard_Enter] = 40,
            [LedId.Keyboard_Escape] = 41,
            [LedId.Keyboard_Backspace] = 42,
            [LedId.Keyboard_Tab] = 43,
            [LedId.Keyboard_Space] = 44,

            [LedId.Keyboard_MinusAndUnderscore] = 45,
            [LedId.Keyboard_EqualsAndPlus] = 46,
            [LedId.Keyboard_BracketLeft] = 47,
            [LedId.Keyboard_BracketRight] = 48,
            [LedId.Keyboard_Backslash] = 49,
            [LedId.Keyboard_NonUsTilde] = 50,
            [LedId.Keyboard_SemicolonAndColon] = 51,
            [LedId.Keyboard_ApostropheAndDoubleQuote] = 52,
            [LedId.Keyboard_GraveAccentAndTilde] = 53,
            [LedId.Keyboard_CommaAndLessThan] = 54,
            [LedId.Keyboard_PeriodAndBiggerThan] = 55,
            [LedId.Keyboard_SlashAndQuestionMark] = 56,

            [LedId.Keyboard_CapsLock] = 57,

            [LedId.Keyboard_F1] = 58,
            [LedId.Keyboard_F2] = 59,
            [LedId.Keyboard_F3] = 60,
            [LedId.Keyboard_F4] = 61,
            [LedId.Keyboard_F5] = 62,
            [LedId.Keyboard_F6] = 63,
            [LedId.Keyboard_F7] = 64,
            [LedId.Keyboard_F8] = 65,
            [LedId.Keyboard_F9] = 66,
            [LedId.Keyboard_F10] = 67,
            [LedId.Keyboard_F11] = 68,
            [LedId.Keyboard_F12] = 69,

            [LedId.Keyboard_PrintScreen] = 70,
            [LedId.Keyboard_ScrollLock] = 71,
            [LedId.Keyboard_PauseBreak] = 72,
            [LedId.Keyboard_Insert] = 73,
            [LedId.Keyboard_Home] = 74,
            [LedId.Keyboard_PageUp] = 75,
            [LedId.Keyboard_Delete] = 76,
            [LedId.Keyboard_End] = 77,
            [LedId.Keyboard_PageDown] = 78,

            [LedId.Keyboard_ArrowRight] = 79,
            [LedId.Keyboard_ArrowLeft] = 80,
            [LedId.Keyboard_ArrowDown] = 81,
            [LedId.Keyboard_ArrowUp] = 82,

            [LedId.Keyboard_NumLock] = 83,
            [LedId.Keyboard_NumSlash] = 84,
            [LedId.Keyboard_NumAsterisk] = 85,
            [LedId.Keyboard_NumMinus] = 86,
            [LedId.Keyboard_NumPlus] = 87,
            [LedId.Keyboard_NumEnter] = 88,
            [LedId.Keyboard_Num1] = 89,
            [LedId.Keyboard_Num2] = 90,
            [LedId.Keyboard_Num3] = 91,
            [LedId.Keyboard_Num4] = 92,
            [LedId.Keyboard_Num5] = 93,
            [LedId.Keyboard_Num6] = 94,
            [LedId.Keyboard_Num7] = 95,
            [LedId.Keyboard_Num8] = 96,
            [LedId.Keyboard_Num9] = 97,
            [LedId.Keyboard_Num0] = 98,
            [LedId.Keyboard_NumPeriodAndDelete] = 99,

            [LedId.Keyboard_NonUsBackslash] = 100,
            [LedId.Keyboard_Application] = 101,

            [LedId.Keyboard_LeftCtrl] = 224,
            [LedId.Keyboard_LeftShift] = 225,
            [LedId.Keyboard_LeftAlt] = 226,
            [LedId.Keyboard_LeftGui] = 227,
            [LedId.Keyboard_RightCtrl] = 228,
            [LedId.Keyboard_RightShift] = 229,
            [LedId.Keyboard_RightAlt] = 230,
            [LedId.Keyboard_RightGui] = 231,

            /*
             Thanks Simon for handing me this list
             RgbBrightnessUp = 0x401
             RgbBrightnessDown = 0x402
             SelectAnalogProfile1 = 0x403
             SelectAnalogProfile2 = 0x404
             SelectAnalogProfile3 = 0x405
             ModeKey = 0x408
             FnKey = 0x409
             DisableKey = 0x40A
             FnLayerLock = 0x40B
             FnKey2 = 0x40C= 
            */
            [LedId.Keyboard_Custom1] = 1027,
            [LedId.Keyboard_Custom2] = 1028,
            [LedId.Keyboard_Custom3] = 1029,
            [LedId.Keyboard_Custom4] = 1032,
            [LedId.Keyboard_Function] = 1033
        };

        internal static Dictionary<short, LedId> HidCodesReversed { get; } = HidCodes.ToDictionary(x => (short)x.Value, x => x.Key);

        // Map HID codes to keyboard row/column positions
        internal static Dictionary<short, (int row, int col)> HidToPosition { get; } = InitializeHidToPositionMap();

        private static Dictionary<short, (int row, int col)> InitializeHidToPositionMap()
        {
            var map = new Dictionary<short, (int row, int col)>();
            
            foreach (var kvp in HidCodesReversed)
            {
                var hidCode = kvp.Key;
                var ledId = kvp.Value;
                var position = GetLedIdPosition(ledId);
                if (position.HasValue)
                {
                    map[hidCode] = position.Value;
                }
            }
            
            return map;
        }

        private static (int row, int col)? GetLedIdPosition(LedId ledId)
        {
            // Map LedId to keyboard row/col positions (0-indexed)
            // Based on standard keyboard layout
            return ledId switch
            {
                // Row 0 - F keys and extras
                LedId.Keyboard_Escape => (0, 0),
                LedId.Keyboard_F1 => (0, 2),
                LedId.Keyboard_F2 => (0, 3),
                LedId.Keyboard_F3 => (0, 4),
                LedId.Keyboard_F4 => (0, 5),
                LedId.Keyboard_F5 => (0, 6),
                LedId.Keyboard_F6 => (0, 7),
                LedId.Keyboard_F7 => (0, 8),
                LedId.Keyboard_F8 => (0, 9),
                LedId.Keyboard_F9 => (0, 10),
                LedId.Keyboard_F10 => (0, 11),
                LedId.Keyboard_F11 => (0, 12),
                LedId.Keyboard_F12 => (0, 13),
                LedId.Keyboard_PrintScreen => (0, 14),
                LedId.Keyboard_ScrollLock => (0, 15),
                LedId.Keyboard_PauseBreak => (0, 16),

                // Row 1 - Number row
                LedId.Keyboard_GraveAccentAndTilde => (1, 0),
                LedId.Keyboard_1 => (1, 1),
                LedId.Keyboard_2 => (1, 2),
                LedId.Keyboard_3 => (1, 3),
                LedId.Keyboard_4 => (1, 4),
                LedId.Keyboard_5 => (1, 5),
                LedId.Keyboard_6 => (1, 6),
                LedId.Keyboard_7 => (1, 7),
                LedId.Keyboard_8 => (1, 8),
                LedId.Keyboard_9 => (1, 9),
                LedId.Keyboard_0 => (1, 10),
                LedId.Keyboard_MinusAndUnderscore => (1, 11),
                LedId.Keyboard_EqualsAndPlus => (1, 12),
                LedId.Keyboard_Backspace => (1, 13),
                LedId.Keyboard_Insert => (1, 14),
                LedId.Keyboard_Home => (1, 15),
                LedId.Keyboard_PageUp => (1, 16),

                // Row 2 - QWERTY row
                LedId.Keyboard_Tab => (2, 0),
                LedId.Keyboard_Q => (2, 1),
                LedId.Keyboard_W => (2, 2),
                LedId.Keyboard_E => (2, 3),
                LedId.Keyboard_R => (2, 4),
                LedId.Keyboard_T => (2, 5),
                LedId.Keyboard_Y => (2, 6),
                LedId.Keyboard_U => (2, 7),
                LedId.Keyboard_I => (2, 8),
                LedId.Keyboard_O => (2, 9),
                LedId.Keyboard_P => (2, 10),
                LedId.Keyboard_BracketLeft => (2, 11),
                LedId.Keyboard_BracketRight => (2, 12),
                LedId.Keyboard_Backslash => (2, 13),
                LedId.Keyboard_Delete => (2, 14),
                LedId.Keyboard_End => (2, 15),
                LedId.Keyboard_PageDown => (2, 16),

                // Row 3 - ASDF row
                LedId.Keyboard_CapsLock => (3, 0),
                LedId.Keyboard_A => (3, 1),
                LedId.Keyboard_S => (3, 2),
                LedId.Keyboard_D => (3, 3),
                LedId.Keyboard_F => (3, 4),
                LedId.Keyboard_G => (3, 5),
                LedId.Keyboard_H => (3, 6),
                LedId.Keyboard_J => (3, 7),
                LedId.Keyboard_K => (3, 8),
                LedId.Keyboard_L => (3, 9),
                LedId.Keyboard_SemicolonAndColon => (3, 10),
                LedId.Keyboard_ApostropheAndDoubleQuote => (3, 11),
                LedId.Keyboard_Enter => (3, 12),

                // Row 4 - ZXCV row
                LedId.Keyboard_LeftShift => (4, 0),
                LedId.Keyboard_Z => (4, 1),
                LedId.Keyboard_X => (4, 2),
                LedId.Keyboard_C => (4, 3),
                LedId.Keyboard_V => (4, 4),
                LedId.Keyboard_B => (4, 5),
                LedId.Keyboard_N => (4, 6),
                LedId.Keyboard_M => (4, 7),
                LedId.Keyboard_CommaAndLessThan => (4, 8),
                LedId.Keyboard_PeriodAndBiggerThan => (4, 9),
                LedId.Keyboard_SlashAndQuestionMark => (4, 10),
                LedId.Keyboard_RightShift => (4, 11),
                LedId.Keyboard_ArrowUp => (4, 14),

                // Row 5 - Bottom row
                LedId.Keyboard_LeftCtrl => (5, 0),
                LedId.Keyboard_LeftGui => (5, 1),
                LedId.Keyboard_LeftAlt => (5, 2),
                LedId.Keyboard_Space => (5, 5),
                LedId.Keyboard_RightAlt => (5, 9),
                LedId.Keyboard_Function => (5, 10),
                LedId.Keyboard_Application => (5, 11),
                LedId.Keyboard_RightCtrl => (5, 12),
                LedId.Keyboard_ArrowLeft => (5, 13),
                LedId.Keyboard_ArrowDown => (5, 14),
                LedId.Keyboard_ArrowRight => (5, 15),

                _ => null
            };
        }
    }
}





