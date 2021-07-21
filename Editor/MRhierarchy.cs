using System.Diagnostics;
using System.Drawing;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;


namespace MR.Hierarchy
{
    [InitializeOnLoad]
    public class MRhierarchy : /*MonoBehaviour*/ Editor
    {
        static Texture2D texturedMR;

        static bool isInited = false;
        static UnityEngine.Color[] defaultGradient = {UnityEngine.Color.red, UnityEngine.Color.blue};

        //types we want to be able to use
        //add new types here and handle them in the switch statement below...
        static private string[] types = { "gr:", "bg:", "b:", "t:", "bs:", "ts:", "tf:", "icon:", "icn:", "ic:"};

        //dict to hold any gradient texture we already created...
        static Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        static MRhierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
        }

        //initialize the png asset...
        static void Initialize()
        {
            if (isInited)
            {
                return;
            }

            string[] guids2 = AssetDatabase.FindAssets("MR_icon_blue_32x32 t:texture2D");

            if (guids2.Length >= 1)
            {
                var pathToPng = AssetDatabase.GUIDToAssetPath(guids2[0]);
                texturedMR = (Texture2D)AssetDatabase.LoadAssetAtPath<Texture2D>(pathToPng);
            }
            else
            {
                UnityEngine.Debug.LogError($"MR_icon_blue_32x32.png NOT FOUND...");
            }

            isInited = true;
        }

        static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (!gameObject)
            {
                return;
            }

            //* This needs to be here (and not inside the if "//" statement) because the
            //* icon drawer needs the BackgroundRect info...
            Rect BackgroundRect = new Rect();

            float xPos  = selectionRect.position.x + 60f - 28f - selectionRect.xMin;
            float yPos  = selectionRect.position.y;
            float xSize = selectionRect.size.x + selectionRect.xMin + 28f - 60 + 16f;
            float ySize = selectionRect.size.y;

            BackgroundRect = new Rect(xPos, yPos, xSize, ySize);

            //if the name of the GO starts with a double "//" i.e. like a comment...
            if (gameObject.name.StartsWith("//", System.StringComparison.Ordinal))
            {
                //*default colours!!
                UnityEngine.Color backGroundColour = UnityEngine.Color.black;
                UnityEngine.Color borderColour     = UnityEngine.Color.white;
                UnityEngine.Color textColour       = UnityEngine.Color.red;
                UnityEngine.Color[] gradientCols   = {UnityEngine.Color.red, UnityEngine.Color.blue};

                float textSize      = 12f;
                float borderSize    = 2f;
                bool borderOn       = false; //draw the border
                bool gradientOn     = false; //draw the gradient
                bool colourOn       = true; //draw the single colour
                bool textOn         = true; //draw the text
                bool iconOn         = false;
                string gradientname = "";

                var fontSt = FontStyle.BoldAndItalic;
                var fontAlign = TextAnchor.MiddleCenter;

                var offset = BackgroundRect;
                gameObject.SetActive(false);

                //make sure "/" is removed even if no types are changed!
                var name = gameObject.name.Replace("/", "");

                //loop through array of known types and do magic...
                foreach (var type in types)
                {
                    (string after, string finalName) = GetStringAfterType(name, type);

                    if (after == null)
                    {
                        //if that type wasn't found in string => bail
                        continue;
                    }

                    //only use the "finalName" if it isn't null
                    //this means we are incrementally removing the type and formatting info
                    //from our name string => only the final string for the LabelField should be left when done
                    name = finalName ?? name;

                    //deal with the types
                    switch (type)
                    {
                        case "gr:":
                            gradientOn = true;
                            borderOn = false;
                            gradientname = after;
                            gradientCols = ConvertStringToColors(after, defaultGradient);
                            offset = BackgroundRect;
                            break;
                        case "bg:":
                            colourOn = true;
                            backGroundColour = ConvertStringToColor(after, UnityEngine.Color.white);
                            break;
                        case "b:":
                            //* "b:" is always checked  AFTER the "bg:"; that's why we can do this now
                            if (after.Equals("="))
                            {
                                //when border "=" (i.e. equals) the background color, simple don't draw border at all
                                borderOn = false;
                                //make sure the backgroundColour Rect is resized properly
                                offset = BackgroundRect;
                            }
                            else
                            {
                                borderOn = true;
                                offset = Shrink(BackgroundRect, borderSize);
                                borderColour = ConvertStringToColor(after, UnityEngine.Color.white);
                            }
                            break;
                        case "t:":
                            textOn = true;
                            textColour = ConvertStringToColor(after, UnityEngine.Color.white);
                            break;
                        case "bs:":
                            borderOn = true;
                            offset = Shrink(BackgroundRect, ConvertStringToFloat(after, 2));
                            break;
                        case "ts:":
                            textSize = ConvertStringToFloat(after, 12);
                            break;
                        case "tf:": //text format
                            (fontSt, fontAlign) = ConvertStringToTextFormat(after);
                            break;
                        case "icon:": //all 3 options are allowed for displaying icon...
                        case "icn:":
                        case "ic:":
                            iconOn = true;
                            break;
                    }
                }
                //----------------------------------
                if (name == null)
                {
                    textOn = false;
                }
                else
                {
                    textOn = true;
                }
                //----------------------------------
                //all variables are set, now draw and put the text
                if (borderOn)
                    EditorGUI.DrawRect(BackgroundRect, borderColour);

                if (colourOn)
                    EditorGUI.DrawRect(offset, backGroundColour);

                if (gradientOn)
                {
                    Texture2D gradient = CreateGradientTexture(4, 4, gradientCols[0], gradientCols[1], gradientname);
                    GUI.DrawTexture(offset, gradient, ScaleMode.StretchToFill);
                }

                //only draw the text if there is text to draw
                if (textOn)
                {
                    EditorGUI.LabelField(BackgroundRect, name, new GUIStyle()
                        {
                            normal = new GUIStyleState() { textColor = textColour },
                            fontStyle = fontSt,
                            fontSize = (int)textSize,
                            wordWrap = true,
                            alignment = fontAlign
                        }
                    );
                }

                if (iconOn) {
                    DrawIcon(BackgroundRect, texturedMR);
                }
            }

            if (gameObject.GetComponent(typeof(IMR)))
            {
                //  Debug.Log($"ICON BackgroundRect: {BackgroundRect}");
                DrawIcon(BackgroundRect, texturedMR);
            }
        }

        private static (FontStyle fontStyle, TextAnchor fontAlign) ConvertStringToTextFormat (string name) {

            var formatting = name.Split(',');

            FontStyle fontSt = FontStyle.BoldAndItalic;
            TextAnchor fontAlign = TextAnchor.MiddleCenter;

            foreach (var format in formatting)
            {
                switch (format)
                {
                    case "i":
                        fontSt = FontStyle.Italic;
                        break;
                    case "b":
                        fontSt = FontStyle.Bold;
                        break;
                    case "ib":
                    case "bi":
                        fontSt = FontStyle.BoldAndItalic;
                        break;
                    case "n":
                        fontSt = FontStyle.Normal;
                        break;
                    case "l":
                        fontAlign = TextAnchor.MiddleLeft;
                        break;
                    case "c":
                        fontAlign = TextAnchor.MiddleCenter;
                        break;
                    case "r":
                        fontAlign = TextAnchor.MiddleRight;
                        break;
                }
            }

            return (fontSt, fontAlign);
        }

        private static (string withoutType, string final) GetStringAfterType(string cString, string type)
        {
            string newStr = "";
            string finalName = "";

            if (type.Equals("icon:") || type.Equals("icn:") || type.Equals("ic:")) {

                //type "icon:" is a switch so there's nothing after it... don't check for spaces!
                finalName = cString;
            }
            else {

                finalName = CheckForWhiteSpaceAfterType(cString, type);
            }

            //remove the "//" since we done't want to see this on the GO in the hierarchy
            finalName = finalName.Replace("/", "");

            //split string into words, delimited by spaces
            var strgs = finalName.Split(' ');

            //loop through words, checking for the "type", return the string after the type and a string
            //containing the type AND the string after it!
            foreach (var str in strgs)
            {
                if (str.Contains(type))
                {
                    //remove any of the filler characters
                    newStr = str.Replace("_", "").Replace(type, "").Replace(" ", "");
                    finalName = finalName.Replace(str, "");

                    return (newStr, finalName);
                }
            }
            return (null, null);
        }

        private static UnityEngine.Color ConvertRGBstringToColor(string noType, UnityEngine.Color defCol) {

            var argb = new string(noType.Where(c => Char.IsDigit(c) || c==',' || c=='.').ToArray()).Split(',');
            List<float> nCol = new List<float>();

            foreach (var col in argb)
            {
                nCol.Add(ConvertStringToFloat(col, 0));
            }

            if (nCol.Count == 3) {

                UnityEngine.Color rgbCol = new UnityEngine.Color(nCol[0], nCol[1], nCol[2]);

                return rgbCol;
            }
            else if (nCol.Count == 4) {
                UnityEngine.Color rgbaCol = new UnityEngine.Color(nCol[0], nCol[1], nCol[2], nCol[3]);

                return rgbaCol;
            }

            return defCol;
        }

        //try to convert the string to a Unity Color...
        private static UnityEngine.Color ConvertStringToColor(string noType, UnityEngine.Color defCol)
        {
            if (noType == null)
            {
                return defCol;
            }

            if (noType.ToLower().Contains("rg") || noType.ToLower().Contains("rga") ||
                noType.ToLower().Contains("rgb:") || noType.ToLower().Contains("rgba:")) {

                return ConvertRGBstringToColor(noType, defCol);
            }
            else {

                if (ColorUtility.TryParseHtmlString(noType, out UnityEngine.Color finalColor))
                {
                    return finalColor;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Color: {noType} could not be converted...");
                    return defCol;
                }
            }
        }

        //split the string of gradient colours appart and return Unity Colors
        private static UnityEngine.Color[] ConvertStringToColors(string noType, UnityEngine.Color[] defCol)
        {
            if (noType == null)
            {
                return defCol;
            }

            List<UnityEngine.Color> colours = new List<UnityEngine.Color>();

            var cols = noType.Split('-');

            foreach (var col in cols)
            {
                if (col.ToLower().Contains("rg") || col.ToLower().Contains("rga") ||
                col.ToLower().Contains("rgb:") || col.ToLower().Contains("rgba:")) {

                    colours.Add(ConvertRGBstringToColor(col, defCol[0]));
                }
                else {

                    if (ColorUtility.TryParseHtmlString(col, out UnityEngine.Color finalColor))
                    {
                        colours.Add(finalColor);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Color: {noType} could not be converted...");
                        return defCol;
                    }
                }
            }

            if (colours.Count == 2)    {

                return colours.ToArray();
            }

            return defCol;
        }

        private static float ConvertStringToFloat(string noType, float def)
        {
            if (Single.TryParse(noType, out float offset))
            {
                return offset;
            }
            return def;
        }

        private static string CheckForWhiteSpaceAfterType(string cString, string type)
        {
            var typeExists = cString.IndexOf(type, System.StringComparison.Ordinal);
            var nameCheck = cString;

            if (typeExists >= 0)
            {
                var typeLength = type.Length;
                var spaceLocation = (typeExists + typeLength);
                var stillSpace = true;

                //remove any whitespace after a "type"; e.g.: "bg:  red" needs to be "bg:red"
                while (stillSpace)
                {
                    if (Char.IsWhiteSpace(nameCheck, spaceLocation))
                    {
                        nameCheck = nameCheck.Remove(spaceLocation, 1);
                    }
                    else
                    {
                        stillSpace = false;
                    }
                }
            }
            return nameCheck;
        }


        //this is for drawing the image... not useful yet...
        private static void DrawIcon(Rect rect, Texture2D icon)
        {

            //look for assets only if we are actually trying to use them...
            Initialize();
            // Debug.Log($"ICON ORIGrect: {selectionRect}");
            rect.x = rect.x + rect.width - 15 - 2f;
            // Debug.Log($"ICONrect: {selectionRect}");

            GUI.Label(rect, icon);
        }

        private static Texture2D CreateGradientTexture(int width, int height, UnityEngine.Color colorOne, UnityEngine.Color colorTwo, string gradientKey)
        {
            //check if texture already exists...
            if(textures.TryGetValue(gradientKey, out Texture2D temp))
            {
                // Debug.Log($"Dict DOES contain {gradientKey}");
                return temp;
            }
            else
            {  //only create a texture if we haven't created it yet...
                // Debug.Log($"Dict doesn't contain {gradientKey} yet... creating...");
                Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
                tex.hideFlags = HideFlags.HideAndDontSave;

                UnityEngine.Color[] color = new UnityEngine.Color[width * height];

                for (int i = 0; i < width; i++)
                {
                    UnityEngine.Color col = UnityEngine.Color.Lerp(colorOne, colorTwo, (float)i / (float)(width - 1)); //(float)(Mathf.Sin(width - 1)));
                    for (int j = 0; j < height; j++)
                    {
                        color[j * width + i] = col;
                    }
                }
                tex.SetPixels(color);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply();

                textures.Add(gradientKey, tex);

                return tex;
            }
        }

        private static Texture2D CreateTextureWithColor(UnityEngine.Color color)
        {

            Texture2D tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;

            tex.SetPixel(0, 0, color);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();

            return tex;
        }

        private static Rect Shrink(Rect self, float offset) {

            self.xMin     += offset;
            self.yMin     += offset;
            self.width    -= offset;
            self.height   -= offset;

            return self;
        }
    }
}