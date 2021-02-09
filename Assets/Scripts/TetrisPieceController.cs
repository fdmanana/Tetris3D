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

public class TetrisPieceController : MonoBehaviour
{
    public enum PieceShape {
        L,
        Z,
        T,
        I,
        O,
        Single
    };

    private enum RotationAxis {
        X,
        Y,
    };

    private float nextDrop;

    // These offsets, for x, y and z, are what we sum into the piece's current
    // position at Start()/Awake() so that the piece is moved to the bottom
    // left and front corner of the game grid. The piece is initially not
    // entirely inside the game grid, because when we create it we need to
    // define its rotation center. These values should be set in the editor for
    // each different piece.
    public float centerOffsetX;
    public float centerOffsetY;
    public float centerOffsetZ;

    // Set in the editor.
    public PieceShape pieceShape;
    public AudioClip moveSFX;
    public AudioClip canNotMoveSFX;

    private bool canRotate = true;
    // Piece can't be moved or rotated, not part of the game.
    private bool inGame = true;

    public void SetUnplayable()
    {
        inGame = false;
    }

    void Start()
    {
        if (!inGame) {
            return;
        }

        bool collision = false;
        float xStart = centerOffsetX;
        float yStart = centerOffsetY;
        float zStart = centerOffsetZ;

        switch (pieceShape) {
            case PieceShape.T:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 2.0f;
                break;
            case PieceShape.I:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 3.0f;
                break;
            case PieceShape.Z:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 2.0f;
                break;
            case PieceShape.L:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 2.0f;
                break;
            case PieceShape.O:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 2.0f;
                break;
            case PieceShape.Single:
                xStart += 2.0f;
                yStart += GameSettings.numberOfPlanes - 1;
                zStart += 3.0f;
                canRotate = false;
                break;
        }

        // If the piece overlaps any previously layed down piece, we have a
        // game over, and we move the piece up until it no longer overlaps,
        // just for visualization purposes.
        transform.Translate(xStart, yStart, zStart, Space.World);
        DumpChildrenGridIndices();
        while (InvalidPosition(true)) {
            transform.Translate(0, 1, 0, Space.World);
            collision = true;
        }

        if (collision) {
            GameManager.gm.GameOver();
        } else if (GridManager.gm.MaybeFreezePiece(gameObject)) {
            GameManager.gm.GameOver();
        }

        nextDrop = 0;
    }

    private string GridIndexString(Transform cube)
    {
        int plane = TetrisHelpers.GetPieceCubePlane(cube);
        int row = TetrisHelpers.GetPieceCubeRow(cube);
        int col = TetrisHelpers.GetPieceCubeColumn(cube);

        return "[" + plane + ", " + row + ", " + col + "]";
    }

    private void DumpChildrenGridIndices()
    {
        if (!GameSettings.debugMode) {
            return;
        }

        Debug.Log("Current position: " + transform.position + " world: " + transform.TransformPoint(transform.position));

        foreach (Transform cube in transform) {

            Debug.Log("cube '" + cube.name + "' index: " + GridIndexString(cube) + " position: " + cube.position);
        }        
    }

    // Check if we can move a piece along the X axis by a certain amount.
    // If any of the cubes goes off the grid, we can not move the piece.
    private bool CanMovePieceAxisX(int amount)
    {
        bool canMove = true;

        foreach (Transform cube in transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);
            int nextRow = row + amount;

            if (nextRow < 0) {
                canMove = false;
                break;
            }
            if (nextRow >= GameSettings.rowsPerPlane) {
                canMove = false;
                break;
            }
            if (GridManager.gm.GridCellOccupied(plane, nextRow, col)) {
                canMove = false;
                break;
            }
        }

        if (canMove) {
            AudioSource.PlayClipAtPoint(moveSFX, transform.position);
        } else {
            AudioSource.PlayClipAtPoint(canNotMoveSFX, transform.position);
        }

        return canMove;
    }

    // Check if we can move a piece along the Z axis by a certain amount.
    // If any of the cubes goes off the grid, we can not move the piece.
    private bool CanMovePieceAxisZ(int amount)
    {
        bool canMove = true;

        foreach (Transform cube in transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);
            int nextCol = col + amount;

            if (nextCol < 0) {
                canMove = false;
                break;
            }
            if (nextCol >= GameSettings.columnsPerPlane) {
                canMove = false;
                break;
            }

            if (GridManager.gm.GridCellOccupied(plane, row, nextCol)) {
                canMove = false;
                break;
            }
        }

        if (canMove) {
            AudioSource.PlayClipAtPoint(moveSFX, transform.position);
        } else {
            AudioSource.PlayClipAtPoint(canNotMoveSFX, transform.position);
        }

        return canMove;
    }

    // Check if we can move a piece along the Y axis by a certain amount.
    // If any of the cubes goes off the grid, we can not move the piece.
    private bool CanMovePieceAxisY(int amount)
    {
        bool canMove = true;

        foreach (Transform child in transform) {
            float sum = child.position.y + amount;

            if (amount < 0 && sum < 0) {
                canMove = false;
                break;
            }
            if (sum >= GameSettings.numberOfPlanes) {
                canMove = false;
                break;
            }
        }

        if (canMove) {
            AudioSource.PlayClipAtPoint(moveSFX, transform.position);
        } else {
            AudioSource.PlayClipAtPoint(canNotMoveSFX, transform.position);
        }

        return canMove;
    }

    private bool InvalidPosition(bool ignoreYPosition = false)
    {
        foreach (Transform cube in transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);

            // One cube of the piece is going outside the game grid...
            if (row < 0 || row >= GameSettings.rowsPerPlane ||
                col < 0 || col >= GameSettings.columnsPerPlane) {
                    return true;
            }

            if (!ignoreYPosition) {
                if (plane < 0 || plane >= GameSettings.numberOfPlanes) {
                    return true;
                }
            }

            if (ignoreYPosition && plane >= GameSettings.numberOfPlanes) {
                continue;
            }

            // One cube of the piece is in a grid position that alreay has a cube
            // from a previous piece...
            if (GridManager.gm.GridCellOccupied(plane, row, col)) {
                return true;
            }
        }

        return false;
    }

    private bool RotatePiece(RotationAxis axis)
    {
        if (!canRotate) {
            return false;
        }

        // We want to see that after the full rotation the piece does not
        // overlap any previously layed down piece. But we also want to check
        // that the transition to the rotation is also possible, that the path
        // followed by the rotation movement is not obstructed. So we first rotate
        // by 45 degrees, check if the position of each cube after that rotation
        // does not overlap any previously layed down pieces, and if not then
        // rotate by another 45 degrees and do the same checks.

        float xAngle = 0;
        float yAngle = 0;

        switch (axis) {
            case RotationAxis.X:
                xAngle = 45;
                break;
            case RotationAxis.Y:
                yAngle = 45;
                break;
        }

        transform.Rotate(xAngle, yAngle, 0, Space.World);

        if (InvalidPosition()) {
            transform.Rotate(-xAngle, -yAngle, 0, Space.World);
            AudioSource.PlayClipAtPoint(canNotMoveSFX, transform.position);
            return false;
        }

        transform.Rotate(xAngle, yAngle, 0, Space.World);

        if (InvalidPosition()) {
            transform.Rotate(-xAngle * 2, -yAngle * 2, 0, Space.World);
            AudioSource.PlayClipAtPoint(canNotMoveSFX, transform.position);
            return false;
        }

        AudioSource.PlayClipAtPoint(moveSFX, transform.position);

        return true;
    }

    private void LayPieceDown()
    {
        do {
            transform.Translate(0, -1, 0, Space.World);
        } while (!GridManager.gm.MaybeFreezePiece(gameObject));

        GameManager.gm.SpawnNextPiece();
    }

    void Update()
    {
        if (!inGame || !GameManager.gm.IsGameActive()) {
            return;
        }

        nextDrop += Time.deltaTime;

        if (nextDrop >= GameManager.gm.GetTimeout()) {
            transform.Translate(0, -1, 0, Space.World);
            nextDrop = 0;
            if (GridManager.gm.MaybeFreezePiece(gameObject)) {
                GameManager.gm.SpawnNextPiece();
                return;
            }
        }

        // Piece moved or rotated.
        bool pieceMoved = false;

        if (Input.GetKeyDown(GameSettings.moveYAxisNegativeKey)) {
            // Move along Y axis in the negative direction (down).
            if (CanMovePieceAxisY(-1)) {
                LayPieceDown();
            }
        } else if (GameSettings.debugMode && Input.GetKeyDown(GameSettings.moveYAxisPositiveKey)) {
            // Move along Y axis in the positive direction (up) - debug mode only.
            if (CanMovePieceAxisY(1)) {
                transform.Translate(0, 1, 0, Space.World);
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.moveXAxisNegativeKey)) {
            // Move along X axis in the negative direction.
            if (CanMovePieceAxisX(-1)) {
                transform.Translate(-1, 0, 0, Space.World);
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.moveXAxisPositiveKey)) {
            // Move along X axis in the positive direction.
            if (CanMovePieceAxisX(1)) {
                transform.Translate(1, 0, 0, Space.World);
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.moveZAxisNegativeKey)) {
            // Move along Z axis in the negative direction.
            if (CanMovePieceAxisZ(-1)) {
                transform.Translate(0, 0, -1, Space.World);
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.moveZAxisPositiveKey)) {
            // Move along Z axis in the positive direction.
            if (CanMovePieceAxisZ(1)) {
                transform.Translate(0, 0, 1, Space.World);
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.rotatePieceXAxis)) {
            // Rotate by +90 degrees on the X axis (if possible).
            pieceMoved = RotatePiece(RotationAxis.X);
        } else if (Input.GetKeyDown(GameSettings.rotatePieceYAxis)) {
            // Rotate by +90 degrees on the Y axis (if possible).
            pieceMoved = RotatePiece(RotationAxis.Y);
        }

        if (pieceMoved) {
                DumpChildrenGridIndices();
                if (GridManager.gm.MaybeFreezePiece(gameObject)) {
                    GameManager.gm.SpawnNextPiece();
                }
        }
    }
}
