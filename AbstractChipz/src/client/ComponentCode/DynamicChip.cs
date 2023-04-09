using System.Text;
using JimmysUnityUtilities;
using LogicAPI.Data.BuildingRequests;
using LogicWorld.BuildingManagement;
using LogicWorld.ClientCode.LabelAlignment;
using LogicWorld.ClientCode;
using LogicWorld.Interfaces;
using LogicWorld.Interfaces.Building;
using LogicWorld.Rendering.Components;
using LogicWorld.References;
using LogicWorld.Rendering.Chunks;
using System.Collections.Generic;
using UnityEngine;
using static LogicWorld.Building.WorldOutliner;

namespace Chipz.ComponentCode
{
    public abstract class DynamicChip : ComponentClientCode, IComponentClientCode
    {
        #region Static Methods
        /// <summary>
        /// Converts a given string of digits and letters to their superscript equivalents
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Superscript(string text)
        {
            // Define the superscript digits and letters
            const string SuperscriptDigits = "\u2070\u00b9\u00b2\u00b3\u2074\u2075\u2076\u2077\u2078\u2079";
            const string SuperscriptLetters = "\u1D2C\u1D2E\u1D9C\u1D30\u1D31\u1DA0\u1D33\u1D34\u1D35\u1D36\u1D37\u1D38\u1D39\u1D3A\u1D3C\u1D3E\uA7F4\u1D3F\u02E2\u1D40\u1D41\u2C7D\u1D42\u02E3\u02B8\u1DBB";

            // Convert the input text into an array of characters
            char[] inputChars = text.ToUpper().ToCharArray();

            // Initialize a StringBuilder to store the superscript text
            StringBuilder superscriptBuilder = new StringBuilder();

            // Iterate through each character in the input text
            for (int i = 0; i < inputChars.Length; i++)
            {
                // Check if the character is a digit (0-9)
                if (char.IsDigit(inputChars[i]))
                {
                    // Append the corresponding superscript digit to the StringBuilder
                    superscriptBuilder.Append(SuperscriptDigits[inputChars[i] - '0']);
                }
                else if (char.IsUpper(inputChars[i]))
                {
                    // Disregard the character 'Q'
                    if (inputChars[i] == 'Q')
                    {
                        continue;
                    }

                    // Append the corresponding superscript capital letter to the StringBuilder
                    superscriptBuilder.Append(SuperscriptLetters[inputChars[i] - 'A']);
                }
            }

            // Convert the modified StringBuilder back to a string
            string superscript = superscriptBuilder.ToString();

            return superscript;
        }
        public struct DataObj : Label.IData
        {
            public string LabelText { get; set; }
            public Color24 LabelColor { get; set; }
            public bool LabelMonospace { get; set; }
            public float LabelFontSizeMax { get; set; }
            public LabelAlignmentHorizontal HorizontalAlignment { get; set; }
            public LabelAlignmentVertical VerticalAlignment { get; set; }
            public int SizeX { get; set; }
            public int SizeZ { get; set; }
        }
        /// <summary>
        /// Creates a text label that's in the proper format for GPU Instancing.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static GameObject CreateTextLabel(DataObj data)
        {
            GameObject GO = Object.Instantiate(Prefabs.ComponentDecorations.LabelText);
            GO.GetComponent<LabelTextManager>().DataUpdate(data);
            return GO;
        }
        #endregion

        public GameObject chipTitleLabel;
        #region Abstract Variables
        public abstract ColoredString ChipTitle { get; }
        #endregion
        #region Virtual Variables
        public struct ColoredString { public Color24 Color; public string Text; }
        public virtual ColoredString GetInputPinShortLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.MiddleGreen,
                Text = i.ToString()
            };
        }
        public virtual ColoredString GetInputPinLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.LightGreen,
                Text = "INPUT"
            };
        }
        public virtual ColoredString GetOutputPinShortLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.MiddleBlue,
                Text = i.ToString()
            };
        }
        public virtual ColoredString GetOutputPinLabel(int i)
        {
            return new ColoredString()
            {
                Color = Color24.LightBlue,
                Text = "OUTPUT"
            };
        }
        #endregion

        #region Utility Methods
        public void RequestPinCountChange(int SizeX, int SizeZ)
        {
            // This is where we need to change our pin count!
            BuildRequestManager.SendBuildRequestWithoutAddingToUndoStack(new BuildRequest_ChangeDynamicComponentPegCounts(Address, SizeX, SizeZ));
            // We also need to make our placements dirty, so we recalculate those
            MarkChildPlacementInfoDirty();
        }
        public void QueueChipTitleUpdate(string hintText = "")
        {
            chipTitleLabel.GetComponent<LabelTextManager>().DataUpdate(new DataObj()
            {
                HorizontalAlignment = LabelAlignmentHorizontal.Center,
                VerticalAlignment = LabelAlignmentVertical.Middle,
                LabelColor = ChipTitle.Color,
                LabelFontSizeMax = 1f,
                LabelMonospace = true,
                LabelText = ChipTitle.Text,
                SizeX = 4,
                SizeZ = 4
            });
        }
        #endregion

        #region Overrides
        protected override ChildPlacementInfo GenerateChildPlacementInfo()
        {
            List<FixedPlacingPoint> Points = new List<FixedPlacingPoint>();

            for (var i = 0; i < InputCount / 2; i++)
            {
                for (var k = 0; k < OutputCount / 2; k++)
                {
                    Points.Add(new FixedPlacingPoint()
                    {
                        Position = new Vector3(i, 1, k)
                    });
                }
            }

            // Generate placements for our size.
            return new ChildPlacementInfo()
            {
                Points = Points.ToArray()
            };
        }
        protected override IList<IDecoration> GenerateDecorations()
        {
            int width = InputCount / 2;
            int height = OutputCount / 2;
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;

            var decorations = new List<IDecoration>();

            for (var i = 0; i < width * 2; i++)
            {
                bool FirstSide = i < width;
                int iModulated = i % width;
                Vector3 PosToPlace = new Vector3(
                    iModulated + (FirstSide ? -0.5f : 0.5f),
                    0.91f,
                    0 + (FirstSide ? -0.7f : height - 0.3f) + (FirstSide ? -0.5f : 0.5f));
                Quaternion RotToSet = Quaternion.Euler(90f, FirstSide ? 0f : 180f, 0f);

                // Add Input Pin #
                var data = GetInputPinShortLabel(i + 1);
                decorations.Add(new Decoration()
                {
                    LocalPosition = PosToPlace * 0.3f,
                    LocalRotation = RotToSet,
                    IncludeInModels = false,
                    DecorationObject = CreateTextLabel(new DataObj()
                    {
                        HorizontalAlignment = LabelAlignmentHorizontal.Center,
                        VerticalAlignment = LabelAlignmentVertical.Middle,
                        LabelColor = data.Color,
                        LabelMonospace = true,
                        LabelFontSizeMax = 0.8f,
                        LabelText = data.Text,
                        SizeX = 1,
                        SizeZ = 1
                    })
                });

                PosToPlace.x = iModulated + (FirstSide ? -0.5f : 0.5f);
                PosToPlace.y = 0.2f;
                PosToPlace.z = FirstSide ? -0.96f : height - 0.04f;
                RotToSet = Quaternion.Euler(0f, FirstSide ? 0f : 180f, 0f);
                // Add Input Pin Text
                data = GetInputPinLabel(i + 1);
                decorations.Add(new Decoration()
                {
                    LocalPosition = PosToPlace * 0.3f,
                    LocalRotation = RotToSet,
                    IncludeInModels = false,
                    DecorationObject = CreateTextLabel(new DataObj()
                    {
                        HorizontalAlignment = LabelAlignmentHorizontal.Center,
                        VerticalAlignment = LabelAlignmentVertical.Middle,
                        LabelColor = data.Color,
                        LabelMonospace = true,
                        LabelFontSizeMax = 0.35f,
                        LabelText = data.Text,
                        SizeX = 1,
                        SizeZ = 1
                    })
                });
            }

            for (var i = 0; i < height * 2; i++)
            {
                bool FirstSide = i < height;
                int iModulated = i % height;
                Vector3 PosToPlace = new Vector3(
                    (FirstSide ? -0.7f : width - 0.3f) + (FirstSide ? -0.5f : 0.5f),
                    0.91f,
                    iModulated + (FirstSide ? 0.5f : -0.5f));
                Quaternion RotToSet = Quaternion.Euler(90f, FirstSide ? 90f : -90f, 0f);
                // Add Output Pin #
                var data = GetOutputPinShortLabel(i + 1);
                decorations.Add(new Decoration()
                {
                    LocalPosition = PosToPlace * 0.3f,
                    LocalRotation = RotToSet,
                    IncludeInModels = false,
                    DecorationObject = CreateTextLabel(new DataObj()
                    {
                        HorizontalAlignment = LabelAlignmentHorizontal.Center,
                        VerticalAlignment = LabelAlignmentVertical.Middle,
                        LabelColor = data.Color,
                        LabelMonospace = true,
                        LabelFontSizeMax = 0.8f,
                        LabelText = data.Text,
                        SizeX = 1,
                        SizeZ = 1
                    })
                });

                PosToPlace.x = FirstSide ? -0.96f : width - 0.04f;
                PosToPlace.y = 0.2f;
                PosToPlace.z = iModulated + (FirstSide ? 0.5f : -0.5f);
                RotToSet = Quaternion.Euler(0f, FirstSide ? 90f : -90f, 0f);
                // Add Output Pin Text
                data = GetOutputPinLabel(i + 1);
                decorations.Add(new Decoration()
                {
                    LocalPosition = PosToPlace * 0.3f,
                    LocalRotation = RotToSet,
                    IncludeInModels = false,
                    DecorationObject = CreateTextLabel(new DataObj()
                    {
                        HorizontalAlignment = LabelAlignmentHorizontal.Center,
                        VerticalAlignment = LabelAlignmentVertical.Middle,
                        LabelColor = data.Color,
                        LabelMonospace = true,
                        LabelFontSizeMax = 0.35f,
                        LabelText = data.Text,
                        SizeX = 1,
                        SizeZ = 1
                    })
                });
            }

            chipTitleLabel = CreateTextLabel(new DataObj()
            {
                HorizontalAlignment = LabelAlignmentHorizontal.Center,
                VerticalAlignment = LabelAlignmentVertical.Middle,
                LabelColor = ChipTitle.Color,
                LabelFontSizeMax = 1f,
                LabelMonospace = true,
                LabelText = ChipTitle.Text,
                SizeX = 4,
                SizeZ = 4
            });



            decorations.Add(new Decoration()
            {
                DecorationObject = chipTitleLabel,
                LocalPosition = new Vector3(halfWidth - 2.5f, 1.01f, halfHeight + 1.5f) * 0.3f,
                LocalRotation = Quaternion.Euler(90, 90, 0),
                IncludeInModels = true
            });
            return decorations;
        }
        #endregion
    }
}
