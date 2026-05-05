#region Using statements

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace Bitgem.VFX.StylisedWater
{
    [AddComponentMenu("Bitgem/Water  Volume (Transforms)")]
    public class WaterVolumeTransforms : WaterVolumeBase
    {
        #region MonoBehaviour events

        private void OnDrawGizmos()
        {
            if (!ShowDebug)
            {
                return;
            }

            // iterate the chldren
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var meshFilter = child.GetComponent<MeshFilter>();

                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var bounds = meshFilter.sharedMesh.bounds;
                    var min = bounds.min;
                    var max = bounds.max;
                    
                    min.Scale(child.localScale);
                    max.Scale(child.localScale);
                    min += child.localPosition;
                    max += child.localPosition;

                    var minX = Mathf.RoundToInt(min.x / TileSize);
                    var minY = Mathf.RoundToInt(min.y / TileSize);
                    var minZ = Mathf.RoundToInt(min.z / TileSize);

                    var maxX = Mathf.RoundToInt(max.x / TileSize);
                    var maxY = Mathf.RoundToInt(max.y / TileSize);
                    var maxZ = Mathf.RoundToInt(max.z / TileSize);

                    var drawPos = new Vector3(minX + (maxX - minX) / 2f, minY + (maxY - minY) / 2f, minZ + (maxZ - minZ) / 2f) * TileSize;
                    var drawSca = new Vector3(maxX - minX, maxY - minY, maxZ - minZ) * TileSize;
                    
                    drawPos += transform.position;
                    drawPos -= new Vector3(TileSize, TileSize, TileSize);

                    Gizmos.DrawWireCube(drawPos, drawSca);
                }
                else
                {
                    // grab the local position/scale
                    var pos = child.localPosition;
                    var sca = child.localScale / TileSize;

                    // fix to the grid
                    var x = Mathf.RoundToInt(pos.x / TileSize);
                    var y = Mathf.RoundToInt(pos.y / TileSize);
                    var z = Mathf.RoundToInt(pos.z / TileSize);

                    var drawPos = new Vector3(x, y, z) * TileSize;
                    var drawSca = new Vector3(Mathf.RoundToInt(sca.x), Mathf.RoundToInt(sca.y), Mathf.RoundToInt(sca.z)) * TileSize;
                    drawPos += drawSca / 2f;
                    drawPos += transform.position;
                    drawPos -= new Vector3(TileSize, TileSize, TileSize);

                    // render as wired volumes
                    Gizmos.DrawWireCube(drawPos, drawSca);
                }
            }
        }

        private void OnTransformChildrenChanged()
        {
            Rebuild();
        }

        #endregion

        #region Public methods

        protected override void GenerateTiles(ref bool[,,] _tiles)
        {
            // iterate the chldren
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var meshFilter = child.GetComponent<MeshFilter>();

                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var bounds = meshFilter.sharedMesh.bounds;
                    var min = bounds.min;
                    var max = bounds.max;
                    
                    min.Scale(child.localScale);
                    max.Scale(child.localScale);
                    min += child.localPosition;
                    max += child.localPosition;

                    var minX = Mathf.RoundToInt(min.x / TileSize);
                    var minY = Mathf.RoundToInt(min.y / TileSize);
                    var minZ = Mathf.RoundToInt(min.z / TileSize);

                    var maxX = Mathf.RoundToInt(max.x / TileSize);
                    var maxY = Mathf.RoundToInt(max.y / TileSize);
                    var maxZ = Mathf.RoundToInt(max.z / TileSize);

                    for (var ix = minX; ix < maxX; ix++)
                    {
                        for (var iy = minY; iy < maxY; iy++)
                        {
                            for (var iz = minZ; iz < maxZ; iz++)
                            {
                                // validate
                                if (ix < 0 || ix >= MAX_TILES_X || iy < 0 || iy >= MAX_TILES_Y || iz < 0 || iz >= MAX_TILES_Z)
                                {
                                    continue;
                                }

                                // add the tile
                                _tiles[ix, iy, iz] = true;
                            }
                        }
                    }
                }
                else
                {
                    // grab the local position/scale
                    var pos = child.localPosition;
                    var sca = child.localScale / TileSize;

                    // fix to the grid
                    var x = Mathf.RoundToInt(pos.x / TileSize);
                    var y = Mathf.RoundToInt(pos.y / TileSize);
                    var z = Mathf.RoundToInt(pos.z / TileSize);

                    // iterate the size of the transform
                    for (var ix = x; ix < x + Mathf.RoundToInt(sca.x); ix++)
                    {
                        for (var iy = y; iy < y + Mathf.RoundToInt(sca.y); iy++)
                        {
                            for (var iz = z; iz < z + Mathf.RoundToInt(sca.z); iz++)
                            {
                                // validate
                                if (ix < 0 || ix >= MAX_TILES_X || iy < 0 | iy >= MAX_TILES_Y || iz < 0 || iz >= MAX_TILES_Z)
                                {
                                    continue;
                                }

                                // add the tile
                                _tiles[ix, iy, iz] = true;
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}