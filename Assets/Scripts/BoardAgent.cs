using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardAgent : MonoBehaviour
{
    private Board board; // assign in inspector
    private Piece piece; // current active piece

    void Awake()
    {
        board = GetComponent<Board>();
        piece = GetComponent<Piece>();
    }



}
