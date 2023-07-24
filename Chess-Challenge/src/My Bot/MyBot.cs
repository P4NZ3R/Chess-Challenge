using ChessChallenge.API;
using Raylib_cs;
using System;
using System.ComponentModel;
using System.Diagnostics;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {


        return GetBestMove(board, 0);
    }

    Move GetBestMove(Board board, int depth = 0)
    {
        Move[] allMoves = board.GetLegalMoves();
        Move bestMove = allMoves[0];
        int bestboardValue = board.IsWhiteToMove ? int.MinValue : int.MaxValue;
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int value = LookUpTableEvaluation(board);
            Debug.WriteLine($"{value} {move.ToString()}");
            if (value * GetMultiplier(board.IsWhiteToMove) >= bestboardValue * GetMultiplier(board.IsWhiteToMove))
            {
                bestMove = move;
                bestboardValue = value;
            }
            board.UndoMove(move);
        }

        return bestMove;
    }

    int GetMultiplier(bool isWhite) => isWhite ? 1 : -1;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int LookUpTableEvaluation(Board board)
    {
        if(board.IsInCheckmate())
            return board.IsWhiteToMove ? int.MinValue : int.MaxValue;

        int boardValue = 0;
        foreach (PieceList piecelist in board.GetAllPieceLists())
        {
            boardValue += GetMultiplier(piecelist.IsWhitePieceList) * pieceValues[(int)piecelist.TypeOfPieceInList] * piecelist.Count;
        }
        return boardValue;
    }

}
