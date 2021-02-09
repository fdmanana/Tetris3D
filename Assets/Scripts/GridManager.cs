// MIT License
//
// Copyright (c) 2021 Filipe Manana <fdmanana@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class GridManager : MonoBehaviour
{
    public static GridManager gm;

    // Must be set in the editor.
    public GameObject cubeExplosionPrefab;
    // Must be set in the editor.
    public AudioClip explosionSFX;

    private GameObject[,,] grid;

    private int[] planeCubes;

    void Awake()
    {
        grid = new GameObject[GameSettings.numberOfPlanes,
                            GameSettings.rowsPerPlane,
                            GameSettings.columnsPerPlane];

        for (int i = 0; i < grid.GetLength(0); i++) {
            for (int j = 0; j < grid.GetLength(1); j++) {
                for (int k = 0; k < grid.GetLength(2); k++) {
                    grid[i, j, k] = null;
                }
            }
        }

        planeCubes = new int[GameSettings.numberOfPlanes];
        for (int i = 0; i < planeCubes.Length; i++) {
            planeCubes[i] = 0;
        }

        if (gm == null) {
            gm = this.gameObject.GetComponent<GridManager>();
        }
    }

    // After a piece is translated or rotated, call this method to find out if the
    // piece is now on top of another or on top of the floor and therefore should
    // be freezed.
    public bool MaybeFreezePiece(GameObject piece)
    {
        bool freeze = false;

        foreach (Transform cube in piece.transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);

            // Touching the floor.
            if (plane == 0) {
                freeze = true;
                break;
            }

            // Cube is on top of a cube from a previously frozen piece.
            if (grid[plane - 1, row, col] != null) {
                freeze = true;
                break;
            }
        }

        if (freeze) {
            FreezePiece(piece);
        }

        return freeze;
    }

    private void FreezePiece(GameObject piece)
    {
        foreach (Transform cube in piece.transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);

            FreezeCube(cube, plane, row, col);
        }

        piece.transform.DetachChildren();
        Destroy(piece);
        ProcessFullPlanes();
    }

    public bool GridCellOccupied(int plane, int row, int col)
    {
        return grid[plane, row, col] != null;
    }

    private void FreezeCube(Transform cube, int plane, int row, int col)
    {
        grid[plane, row, col] = cube.gameObject;
        planeCubes[plane]++;
        Assert.IsTrue(planeCubes[plane] <= GameSettings.cubesPerPlane);
    }

    private void ProcessFullPlanes()
    {
        int scoreMultiplier = 0;
        int planes = 0;
        int score;

        for (int i = 0; i < planeCubes.Length; i++) {
            if (planeCubes[i] == GameSettings.cubesPerPlane) {
                scoreMultiplier++;
                planes++;
            }
        }

        if (planes == 0) {
            return;
        }

        score = GameSettings.scorePerPlane * planes * scoreMultiplier;

        while (planes > 0) {
            int i = -1;

            for (i = planeCubes.Length - 1; i >= 0; i--) {
                if (planeCubes[i] == GameSettings.cubesPerPlane) {
                    DestroyPlane(i);
                    break;
                }
            }

            Assert.IsTrue(i >= 0);
            planes--;
        }

        GameManager.gm.IncrementScore(score);
    }

    private void DestroyPlane(int plane)
    {
        // Destroy all cubes in the given place.
        for (int i = 0; i < grid.GetLength(1); i++) {
            for (int j = 0; j < grid.GetLength(2); j++) {
                GameObject cube = grid[plane, i, j]; 

                Assert.IsNotNull(cube);
                Instantiate(cubeExplosionPrefab, cube.transform.position, cube.transform.rotation);
                Destroy(cube);
                grid[plane, i, j] = null;
            }
        }
        // We play the explosion once per plane and do not add it to the explosion
        // prefab because it would be too loud - one explosion sound play per cube!
        AudioSource.PlayClipAtPoint(explosionSFX, transform.position);
        planeCubes[plane] = 0;

        // Move all pieces above down by 1 plane.
        for (int i = plane + 1; i < grid.GetLength(0); i++) {
            for (int j = 0; j < grid.GetLength(1); j++) {
                for (int k = 0; k < grid.GetLength(2); k++) {
                    GameObject cube = grid[i, j, k];

                    if (cube != null) {
                        grid[i - 1, j, k] = cube;
                        grid[i, j, k] = null;
                        cube.transform.Translate(0, -1, 0, Space.World);
                    }
                }
            }
            planeCubes[i - 1] = planeCubes[i];
            planeCubes[i] = 0;
        }
    }
}
