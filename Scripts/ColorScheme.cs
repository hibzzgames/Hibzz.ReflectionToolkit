using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hibzz.ReflectionToolkit
{
    public class ColorScheme
    {
        // major badge const values
        const float MAJOR_BADGE_SATURATION = 0.7f;
        const float MAJOR_BADGE_VALUE = 0.7f;

        // minor badge const values
        const float MINOR_BADGE_SATURATION = 0.7f;
        const float MINOR_BADGE_VALUE = 0.55f;

        public static Color Assembly => Color.HSVToRGB(0.59f, MAJOR_BADGE_SATURATION, MAJOR_BADGE_VALUE);
        public static Color Type => Color.HSVToRGB(0.73f, MAJOR_BADGE_SATURATION, MAJOR_BADGE_VALUE);

        public static Color Public => Color.HSVToRGB(0.6f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Internal => Color.HSVToRGB(0.65f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color ProtectedInternal => Color.HSVToRGB(0.675f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Protected => Color.HSVToRGB(0.7f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color PrivateProtected => Color.HSVToRGB(0.725f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Private => Color.HSVToRGB(0.75f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);

        public static Color Static => Color.HSVToRGB(0.05f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Sealed => Color.HSVToRGB(0.075f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Abstract => Color.HSVToRGB(0.1f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);

        public static Color Field       => Color.HSVToRGB(0.2f,  MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Property    => Color.HSVToRGB(0.25f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Method      => Color.HSVToRGB(0.3f,  MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Constructor => Color.HSVToRGB(0.35f, MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
        public static Color Event       => Color.HSVToRGB(0.4f,  MINOR_BADGE_SATURATION, MINOR_BADGE_VALUE);
    }
}
