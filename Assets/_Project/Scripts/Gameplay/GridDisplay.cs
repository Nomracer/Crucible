using System;
using Crucible.Diagnostics;
using Unity.Collections;
using UnityEngine;

namespace Crucible.Gameplay
{
    /// <summary>
    /// The entire play area is one texture on one quad. There are no per-particle objects, so this
    /// is the only thing the renderer ever sees — the whole grid costs a single draw call.
    ///
    /// This is where the managed half of drawing lives. The pixel conversion itself is pure and
    /// stays in the simulation layer so it can become a Burst job later without moving.
    /// </summary>
    public sealed class GridDisplay : IDisposable
    {
        private Texture2D _texture;
        private Material _material;
        private Transform _quad;
        private Camera _camera;

        private int _width;
        private int _height;
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        public void Initialise(Camera camera, MeshRenderer renderer, Shader shader, int width, int height)
        {
            _camera = camera;
            _quad = renderer.transform;
            _width = width;
            _height = height;

            _texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false)
            {
                name = "Crucible Grid",
                // Point filtering: cells are meant to read as cells. Bilinear would smear the
                // grain jitter into mush and cost bandwidth doing it.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                hideFlags = HideFlags.DontSave
            };

            _material = new Material(shader)
            {
                name = "Crucible Grid",
                mainTexture = _texture,
                hideFlags = HideFlags.DontSave
            };

            renderer.sharedMaterial = _material;

            _quad.localScale = new Vector3(width, height, 1f);
            _quad.localPosition = Vector3.zero;

            FitCamera();
        }

        public void Upload(NativeArray<Color32> pixels)
        {
            using (ProfilerMarkers.Upload.Auto())
            {
                _texture.SetPixelData(pixels, 0);
                // No mipmaps to rebuild, and the texture stays readable because it is rewritten
                // every frame.
                _texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            }
        }

        /// <summary>
        /// Re-fits the camera when the screen changes. Orientation is locked, so in practice this
        /// only fires on the first frame and when an editor game view is resized.
        /// </summary>
        public void TickScreen()
        {
            if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
            {
                return;
            }

            FitCamera();
        }

        private void FitCamera()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;

            _camera.orthographic = true;

            float gridAspect = (float)_width / _height;
            float screenAspect = _lastScreenHeight > 0
                ? (float)_lastScreenWidth / _lastScreenHeight
                : gridAspect;

            // Fit the whole grid on screen. Height is derived from the screen aspect and then
            // snapped to the chunk size, so the two aspects are close but not identical; the
            // leftover is a thin letterbox rather than a stretched image.
            _camera.orthographicSize = screenAspect < gridAspect
                ? _width / screenAspect * 0.5f
                : _height * 0.5f;
        }

        public void Dispose()
        {
            if (_material != null)
            {
                UnityEngine.Object.Destroy(_material);
                _material = null;
            }

            if (_texture != null)
            {
                UnityEngine.Object.Destroy(_texture);
                _texture = null;
            }
        }
    }
}
