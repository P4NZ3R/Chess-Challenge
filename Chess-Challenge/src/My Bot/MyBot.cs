using ChessChallenge.API;
using Raylib_cs;
using System;
using System.ComponentModel;
using System.Diagnostics;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int depth = 6;

        int numOfpieces = CountBitsSetTo1(board.AllPiecesBitboard);
        if(numOfpieces > 26)
        {
            depth = Math.Min(depth, 2);
        }
        else if(numOfpieces > 16)
        {
            depth = Math.Min(depth, 3);
        }
        else if(numOfpieces > 10)
        {
            depth = Math.Min(depth, 4);
        }
        else if (numOfpieces > 6)
        {
            depth = Math.Min(depth, 5);
        }

        if (timer.MillisecondsRemaining > 10000)
        {
            depth = Math.Min(depth, 2);
        }
        else if (timer.MillisecondsRemaining > 5000)
        {
            depth = Math.Min(depth, 1);
        }


        return GetBestMove(board, depth).bestMove;
    }

    public static int CountBitsSetTo1(ulong number)
    {
        int count = 0;
        while (number > 0)
        {
            count += (int)(number & 1); // Check the rightmost bit and add it to the count
            number >>= 1; // Shift the number one bit to the right
        }
        return count;
    }

    (Move bestMove, int bestValue) GetBestMove(Board board, int depth = 0)
    {
        Move[] allMoves = board.GetLegalMoves();
        Move bestMove = allMoves[0];
        int bestboardValue = board.IsWhiteToMove ? int.MinValue : int.MaxValue;
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int value = 0;
            if (depth <= 0 || board.IsDraw() || board.IsInCheckmate())
            {
                value = LookUpTableEvaluation(board);
                board.UndoMove(move);
            }
            else
            {
                (Move localBestMove, value) = GetBestMove(board, depth - 1);
                board.UndoMove(move);
            }

            if (value * GetMultiplier(board.IsWhiteToMove) >= bestboardValue * GetMultiplier(board.IsWhiteToMove))
            {
                bestMove = move;
                bestboardValue = value;
            }
        }

        return (bestMove, bestboardValue);
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
            for (int i = 0; i < piecelist.Count; i++)
            {
                boardValue += GetPieceValue(board, piecelist.GetPiece(i));
            }
        }
        return boardValue;
    }

    int GetPieceValue(Board board, Piece piece)
    {
        int value = pieceValues[(int)piece.PieceType];
        switch (piece.PieceType)
        {
            case PieceType.Pawn:
                int pawnValue = (piece.IsWhite ? piece.Square.Rank - 1 : -piece.Square.Rank + 6);
                value += pawnValue * pawnValue;
                break;
            case PieceType.Knight:
            case PieceType.Bishop:
                //float knightValue = (float)Math.Abs((3.5*3.5)-(Math.Abs(piece.Square.Rank - 3.5) * Math.Abs(piece.Square.File - 3.5)));
                //value += (int)knightValue;
                break;
                break;
            case PieceType.Rook:
                break;
            case PieceType.Queen:
                break;
            case PieceType.King:
                break;
        }

        return GetMultiplier(piece.IsWhite) * value;
    }
}
