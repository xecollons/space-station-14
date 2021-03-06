﻿using System.Linq;
using SS14.Client.Interfaces.GameObjects;
using SS14.Client.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.Placement.Modes
{
    public class AlignSimilar : PlacementMode
    {
        private const uint SnapToRange = 50;

        public AlignSimilar(PlacementManager pMan) : base(pMan) { }

        public override void AlignPlacementMode(ScreenCoordinates mouseScreen)
        {
            MouseCoords = ScreenToPlayerGrid(mouseScreen);
            CurrentTile = MouseCoords.Grid.GetTile(MouseCoords);

            if (pManager.CurrentPermission.IsTile)
            {
                return;
            }

            if (!RangeCheck(MouseCoords))
            {
                return;
            }

            var manager = IoCManager.Resolve<IClientEntityManager>();

            var snapToEntities = manager.GetEntitiesInRange(MouseCoords, SnapToRange)
                .Where(entity => entity.Prototype == pManager.CurrentPrototype && entity.Transform.MapID == MouseCoords.MapID)
                .OrderBy(entity => (entity.Transform.WorldPosition - MouseCoords.ToWorld().Position).LengthSquared)
                .ToList();

            if (snapToEntities.Count == 0)
            {
                return;
            }

            var closestEntity = snapToEntities[0];
            if (!closestEntity.TryGetComponent<ISpriteComponent>(out var component))
            {
                return;
            }

            var closestBounds = component.BaseRSI.Size;

            var closestRect =
                Box2.FromDimensions(
                    closestEntity.Transform.WorldPosition.X - closestBounds.X / 2f,
                    closestEntity.Transform.WorldPosition.Y - closestBounds.Y / 2f,
                    closestBounds.X, closestBounds.Y);

            var sides = new[]
            {
                new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Top - closestBounds.Y / 2f),
                new Vector2(closestRect.Left + closestRect.Width / 2f, closestRect.Bottom + closestBounds.Y / 2f),
                new Vector2(closestRect.Left - closestBounds.X / 2f, closestRect.Top + closestRect.Height / 2f),
                new Vector2(closestRect.Right + closestBounds.X / 2f, closestRect.Top + closestRect.Height / 2f)
            };

            var closestSide =
                (from Vector2 side in sides orderby (side - MouseCoords.Position).LengthSquared select side).First();

            MouseCoords = new GridLocalCoordinates(closestSide, MouseCoords.Grid);
        }

        public override bool IsValidPosition(GridLocalCoordinates position)
        {
            if (pManager.CurrentPermission.IsTile)
            {
                return false;
            }
            else if (!RangeCheck(position))
            {
                return false;
            }
            else if (IsColliding(position))
            {
                return false;
            }

            return true;
        }
    }
}
