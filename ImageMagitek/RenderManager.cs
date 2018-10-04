﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ImageMagitek.Codec;
using ImageMagitek.Project;

namespace ImageMagitek
{
    // RenderManager
    // Class that is responsible for rendering an Arranger into a bitmap

    public class RenderManager
    {
        public Image<Argb32> Image { get; set; }
        bool NeedsRedraw = true;

        /// <summary>
        /// Renders an image using the specified arranger
        /// Invalidate must be called to force a new render
        /// </summary>
        /// <param name="arranger"></param>
        /// <returns></returns>
        public bool Render(Arranger arranger)
        {
            if (arranger is null)
                throw new ArgumentNullException();
            if (arranger.ArrangerPixelSize.Width <= 0 || arranger.ArrangerPixelSize.Height <= 0)
                throw new ArgumentException();

            if (Image is null || arranger.ArrangerPixelSize.Height != Image.Height || arranger.ArrangerPixelSize.Width != Image.Width)
                Image = new Image<Argb32>(arranger.ArrangerPixelSize.Width, arranger.ArrangerPixelSize.Height);

            if(Image is null)
                throw new InvalidOperationException();

            if (!NeedsRedraw)
                return true;

            // TODO: Consider using Tile Cache

            FileStream fs = null;
            bool isSequential = arranger is SequentialArranger;

            if(isSequential) // Sequential requires only one seek per render
            {
                fs = arranger.GetResourceRelative<DataFile>(arranger.ElementGrid[0, 0].DataFileKey).Stream;
                fs.Seek(arranger.ElementGrid[0, 0].FileAddress.FileOffset, SeekOrigin.Begin); // TODO: Fix for bitwise
            }

            string prevFileKey = "";

            for(int y = 0; y < arranger.ArrangerElementSize.Height; y++)
            {
                for(int x = 0; x < arranger.ArrangerElementSize.Width; x++)
                {
                    ArrangerElement el = arranger.ElementGrid[x, y];
                    if (!isSequential) // Non-sequential requires a seek for each element rendered
                    {
                        if (el.IsBlank())
                        {
                            GraphicsCodec.DecodeBlank(Image, el);
                            continue;
                        }
                        if(prevFileKey != el.DataFileKey) // Only create a new binary reader when necessary
                            fs = arranger.GetResourceRelative<DataFile>(el.DataFileKey).Stream;

                        fs.Seek(el.FileAddress.FileOffset, SeekOrigin.Begin); // TODO: Fix for bitwise seeking
                        prevFileKey = el.DataFileKey;
                    }

                     GraphicsCodec.Decode(Image, el);
                }
            }

            NeedsRedraw = false;

            return true;
        }

        /// <summary>
        /// Saves the currently edited image to the underlying source using the specified arranger for placement and encoding
        /// </summary>
        /// <param name="arranger"></param>
        /// <returns></returns>
        public bool SaveImage(Arranger arranger)
        {
            if (arranger is null)
                throw new ArgumentNullException();

            if (arranger.ArrangerPixelSize.Width <= 0 || arranger.ArrangerPixelSize.Height <= 0 || arranger.Mode == ArrangerMode.SequentialArranger)
                throw new ArgumentException();

            if (Image is null || arranger.ArrangerPixelSize.Width != Image.Width || arranger.ArrangerPixelSize.Height != Image.Height)
                throw new InvalidOperationException();

            if (Image is null)
                throw new NullReferenceException();

            string PrevFileKey = "";

            FileStream fs = null; // Used for seeking the DataFile associated with an ArrangerElement before encoding it
            bool IsSequential = false;

            if (arranger is SequentialArranger seqArranger) // Seek to the first element
            {
                IsSequential = true;
                fs = arranger.GetResourceRelative<DataFile>(arranger.ElementGrid[0, 0].DataFileKey).Stream;
                fs.Seek(seqArranger.GetInitialSequentialFileAddress().FileOffset, SeekOrigin.Begin); // TODO: Fix for bitwise seeking
            }

            for (int y = 0; y < arranger.ArrangerElementSize.Height; y++)
            {
                for (int x = 0; x < arranger.ArrangerElementSize.Width; x++)
                {
                    ArrangerElement el = arranger.ElementGrid[x, y];
                    if (!IsSequential) // Non-sequential requires a seek for each element rendered
                    {
                        if (el.FormatName == "") // Empty format means a blank tile
                            continue;

                        if (PrevFileKey != el.DataFileKey) // Only get a new FileStream when necessary
                            fs = arranger.GetResourceRelative<DataFile>(el.DataFileKey).Stream;

                        fs.Seek(el.FileAddress.FileOffset, SeekOrigin.Begin); // TODO: Fix for bitwise seeks
                        PrevFileKey = el.DataFileKey;
                    }

                    GraphicsCodec.Encode(Image, el);
                }
            }

            return true;
        }

        /// <summary>
        /// Forces a redraw for next Render call
        /// </summary>
        public void Invalidate()
        {
            NeedsRedraw = true;
        }

        /// <summary>
        /// Gets the local color of a pixel at the specified coordinate
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <returns>Local color</returns>
        public Argb32 GetPixel(int x, int y)
        {
            if (Image is null)
                throw new NullReferenceException();

            return Image[x, y];
        }

        /// <summary>
        /// Sets the pixel of the image to a specified color
        /// </summary>
        /// <param name="x">x-coordinate of pixel</param>
        /// <param name="y">y-coordinate of pixel</param>
        /// <param name="color">Local color to set</param>
        public void SetPixel(int x, int y, Argb32 color)
        {
            if (Image is null)
                throw new NullReferenceException();

            Image[x, y] = color;
        }

    }
}
