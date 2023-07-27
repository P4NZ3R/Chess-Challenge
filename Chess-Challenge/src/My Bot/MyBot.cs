using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    Board board;

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        MonteCarloTreeSearch mcts = new MonteCarloTreeSearch(board);
        mcts.SetRoot(new Node(board, null, new Move(), false));
        return mcts.RunMCTS(timer, 1000);
    }

    public class Node
    {
        public Board Board { get; }
        public Node Parent { get; }
        public List<Node> Children { get; }
        public int Visits { get; set; }
        public double Wins { get; set; }
        public Move Move { get; }
        public bool IsWhiteMove { get; }

        public Node(Board board, Node parent, Move move, bool isMaxPlayer)
        {
            Board = board;
            Parent = parent;
            Children = new List<Node>();
            Visits = 0;
            Wins = 0;
            Move = move;
            IsWhiteMove = isMaxPlayer;
        }

        public bool IsFullyExpanded()
        {
            // For simplicity, we assume all possible moves are children
            return Children.Count == Board.GetLegalMoves().Length;
        }

        public Node GetBestChild()
        {
            // Choose the best child based on the UCT (Upper Confidence Bound for Trees) formula
            double maxUctValue = double.MinValue;
            Node bestChild = null;
            foreach (var child in Children)
            {
                double uctValue = (child.Wins / child.Visits) +
                                  Math.Sqrt(2 * Math.Log(Visits) / child.Visits);
                if (uctValue > maxUctValue)
                {
                    maxUctValue = uctValue;
                    bestChild = child;
                }
            }
            return bestChild;
        }

        public Node SelectUnexploredChild()
        {
            // Select an unexplored child (i.e., a move not tried yet)
            Move[] possibleMoves = Board.GetLegalMoves();
            foreach (var move in possibleMoves)
            {
                if (Children.FirstOrDefault(child => child.Move == move) == null)
                {
                    return new Node(Board, this, move, !IsWhiteMove);
                }
            }
            return null; // All children are explored, this should not happen in Tic Tac Toe
        }
    }

    public class MonteCarloTreeSearch
    {
        private Board board;
        private Node root;
        private Random random;

        public MonteCarloTreeSearch(Board board)
        {
            this.board = board;
            root = null;
            random = new Random();
        }

        public Move RunMCTS(Timer timer, int timeToThinkInMilliseconds)
        {
            if (root == null)
            {
                throw new InvalidOperationException("MCTS cannot run without a root node.");
            }

            while(timer.MillisecondsElapsedThisTurn < timeToThinkInMilliseconds) 
            {
                Node selectedNode = Selection(root);
                Node expandedNode = Expansion(selectedNode);
                double simulationResult = Simulation(expandedNode);
                Backpropagation(expandedNode, simulationResult);
            }

            // Choose the best move based on the most visited child
            Node bestChild = root.Children.OrderByDescending(child => child.Visits).FirstOrDefault();
            return bestChild.Move;
        }

        private Node Selection(Node node)
        {
            while (node.Children.Count > 0)
            {
                if (!node.IsFullyExpanded())
                {
                    return Expansion(node);
                }
                node = node.GetBestChild();
            }
            return node;
        }

        private Node Expansion(Node node)
        {
            Node unexploredChild = node.SelectUnexploredChild();
            if (unexploredChild == null)
            {
                return node; // All children are explored, this should not happen in Tic Tac Toe
            }
            node.Children.Add(unexploredChild);
            return unexploredChild;
        }

        private double Simulation(Node node)
        {
            Stack<Move> movesMade = new Stack<Move>();
            while(!board.IsDraw() && !board.IsInCheckmate())
            {
                Move[] moves = board.GetLegalMoves();
                Move move = moves[random.Next(moves.Length)];
                board.MakeMove(move);
                movesMade.Push(move);
            }
            int eval = 0;
            if (board.IsDraw())
                eval = 0;
            if (board.IsInCheckmate())
                eval = board.IsWhiteToMove ? -1000000 : 1000000;

            while(movesMade.Count > 0)
            {
                board.UndoMove(movesMade.Pop());
            }
            
            return eval;
        }

        private void Backpropagation(Node node, double result)
        {
            while (node != null)
            {
                node.Visits++;
                node.Wins += result;
                node = node.Parent;
            }
        }

        public void SetRoot(Node node)
        {
            root = node;
        }
    }
}
