using Crucible.Core;
using Crucible.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Crucible.Gameplay
{
    /// <summary>
    /// Turns a pressed pointer into stamped cells.
    ///
    /// A plain class rather than a MonoBehaviour: it has no lifecycle of its own and the driver
    /// already runs once per frame, so giving it an <c>Update</c> would only add a managed-to-native
    /// transition for nothing. <see cref="Pointer.current"/> covers both touch and the editor mouse,
    /// so there is no separate desktop path.
    /// </summary>
    public sealed class BrushInput
    {
        private Camera _camera;
        private int _gridWidth;
        private int _gridHeight;

        public byte Element = Elements.Sand;
        public int Radius = 3;

        public void Initialise(Camera camera, int gridWidth, int gridHeight)
        {
            _camera = camera;
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
        }

        public void Tick(ref SandGrid grid, uint tick)
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || !pointer.press.isPressed)
            {
                return;
            }

            Vector2 screenPosition = pointer.position.ReadValue();
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));

            // The quad is centred on the origin and scaled to the grid, so world units are cells.
            int cellX = Mathf.FloorToInt(world.x + _gridWidth * 0.5f);
            int cellY = Mathf.FloorToInt(world.y + _gridHeight * 0.5f);

            // Allow the centre to sit slightly outside so a stroke along the edge still paints the
            // border cells; the stamp clips per-cell anyway.
            if (cellX < -Radius || cellX > _gridWidth + Radius ||
                cellY < -Radius || cellY > _gridHeight + Radius)
            {
                return;
            }

            GridBrush.Stamp(ref grid, cellX, cellY, Radius, Element, tick);
        }
    }
}
