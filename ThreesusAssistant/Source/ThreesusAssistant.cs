﻿using System;
using System.Collections.Generic;
using Threesus.Bots;
using Threesus.CoreGame;

namespace Threesus
{
	/// <summary>
	/// An assistant that runs a Threes AI for the purposes of assisting the player play the actual game of Threes.
	/// </summary>
	static class ThreesusAssistant
	{
		private static readonly IBot _bot = new StandardBotFramework(6, 3, BoardQualityEvaluators.OpennessMatthew);

		/// <summary>
		/// Main application entry point.
		/// </summary>
		private static void Main()
		{
			// Build the board and initialize the deck.
			Deck deck = new Deck(new Rand());
			Board board = new Board();
			Console.WriteLine("Let's initialize the board...");
			Console.WriteLine("The format for each line should be four comma-delimited numbers, using a 0 for an empty space");
			for(int y = 0; y < board.Height; y++)
			{
				Console.Write("Enter row {0}: ", y);
				String[] rows = Console.ReadLine().Split(',');
				if(rows.Length != board.Width)
				{
					Console.WriteLine("Invalid length of entered row.");
					y--;
					continue;
				}

				for(int x = 0; x < board.Width; x++)
				{
					Card card = GetCardFromString(rows[x], false);
					if(card != null)
					{
						board[x, y] = card;
						deck.RemoveCard(card.Value);
					}
				}
			}
			Console.WriteLine("Board and deck successfully initialized.");

			Stack<Board> boardsStack = new Stack<Board>();
			Stack<Deck> decksStack = new Stack<Deck>();

			// Now let's play!
			while(true)
			{
			redo:

				// Print the current board status.
				Console.WriteLine("--------------------");
				for(int y = 0; y < board.Height; y++)
				{
					for(int x = 0; x < board.Width; x++)
					{
						Card c = board[x, y];
						if(c != null)
							Console.Write("{0},", c.Value);
						else
							Console.Write(" ,");
					}
					Console.WriteLine();
				}
				Console.WriteLine("--------------------");
				Console.WriteLine("Current total score: {0}", board.GetTotalScore());

				// Get the next card.
				Console.Write("What is the next card? ");
				string nextCardStr;
				Card nextCard;
				do
				{
					nextCardStr = Console.ReadLine();
					if(nextCardStr == "undo")
					{
						board = boardsStack.Pop();
						deck = decksStack.Pop();
						goto redo;
					}
				}
				while(nextCardStr.Length != 1 || (nextCard = GetCardFromChar(nextCardStr[0], true)) == null);
				NextCardHint nextCardHint = GetNextCardHint(nextCard);

				// Choose a move.
				Console.Write("Thinking...");
				ShiftDirection? aiDir = _bot.GetNextMove(new FastBoard(board), new FastDeck(deck), nextCardHint);
				if(aiDir != null)
					Console.WriteLine("\nSWIPE {0}.", aiDir.Value.ToString().ToUpper());
				else
				{
					Console.WriteLine("NO MORE MOVES.");
					break;
				}

				// Confirm the swipe.
				ShiftDirection? actualDir = aiDir.Value;
				/*do
				{
					Console.Write("What direction did you swipe in? (l, r, u, d, or just hit enter for the suggested swipe) ");
					string dirStr = Console.ReadLine();
					actualDir = GetShiftDirection(dirStr, aiDir.Value);
				}
				while(actualDir == null);*/
				List<IntVector2D> newCardCells = new List<IntVector2D>();
				board.Shift(actualDir.Value, newCardCells);

				// Get the new card location.
				int newCardIndex;
				if(newCardCells.Count > 1)
				{
					Console.WriteLine("Here are the locations where a new card might have been inserted:");
					for(int y = 0; y < board.Height; y++)
					{
						for(int x = 0; x < board.Width; x++)
						{
							int index = newCardCells.IndexOf(new IntVector2D(x, y));
							if(index >= 0)
								Console.Write((char)('a' + index));
							else
								Console.Write('.');
						}
						Console.WriteLine();
					}
					Console.Write("Where was it actually inserted? ");
					do
					{
						string indexStr = Console.ReadLine();
						if(indexStr.Length == 1)
							newCardIndex = indexStr[0] - 'a';
						else
							newCardIndex = -1;
					}
					while(newCardIndex < 0 || newCardIndex >= newCardCells.Count);
				}
				else
				{
					newCardIndex = 0;
				}

				// Get new card value.
				int newCardValue;
				if(nextCardHint == NextCardHint.Bonus)
				{
					do
					{
						Console.Write("!!! What is the value of the new card? ");
					}
					while(!TryGetNewCardValue(Console.ReadLine(), out newCardValue));
				}
				else
				{
					newCardValue = (int)nextCardHint + 1;
				}
				deck.RemoveCard(newCardValue);
				board[newCardCells[newCardIndex]] = new Card(newCardValue, -1);

				boardsStack.Push(new Board(board));
				decksStack.Push(new Deck(deck));
			}

			Console.WriteLine("FINAL SCORE IS {0}.", board.GetTotalScore());
		}

		/// <summary>
		/// Gets the card that is indicated by the specified character.
		/// </summary>
		private static Card GetCardFromString(string c, bool allowBonusCard)
		{
			switch(c)
			{
			case "1":
					return new Card(1, -1);
			case "2":
					return new Card(2, -2);
			case "3":
					return new Card(3, -1);
			case "+":
					if(allowBonusCard)
						return new Card(-1, -1);
					else
						return null;
			case "6":
					return new Card (6, -1);
			case "12":
					return new Card (12, -1);
			case "24":
					return new Card (24, -1);
			case "48":
					return new Card (48, -1);
			case "96":
					return new Card (96, -1);
			case "192":
					return new Card (192, -1);
			case "384":
					return new Card (384, -1);
			case "768":
					return new Card (768, -1);
			case "1536":
					return new Card (1536, -1);
			case "3072":
					return new Card (3072, -1);
			case "6144":
					return new Card (6144, -1);
				default:
					return null;
			}
		}

		private static Card GetCardFromChar(char c, bool allowBonusCard)
		{
			switch(c)
			{
			case '1':
				return new Card(1, -1);
			case '2':
				return new Card(2, -2);
			case '3':
				return new Card(3, -1);
			case '+':
				if(allowBonusCard)
					return new Card(-1, -1);
				else
					return null;
			default:
				return null;
			}
		}

		/// <summary>
		/// Returns the NextCardHint given the specified next card.
		/// </summary>
		private static NextCardHint GetNextCardHint(Card nextCard)
		{
			NextCardHint nextCardHint;
			switch(nextCard.Value)
			{
				case 1:
					nextCardHint = NextCardHint.One;
					break;
				case 2:
					nextCardHint = NextCardHint.Two;
					break;
				case 3:
					nextCardHint = NextCardHint.Three;
					break;
				default:
					nextCardHint = NextCardHint.Bonus;
					break;
			}
			return nextCardHint;
		}

		/// <summary>
		/// Returns the shift direction as specified by the specified string, or null if none was specified.
		/// If the string has no length, then the defaultDir will be returned.
		/// </summary>
		private static ShiftDirection? GetShiftDirection(string str, ShiftDirection defaultDir)
		{
			if(str.Length == 0)
				return defaultDir;
			else if(str.Length > 1)
				return null;
			else
			{
				switch(str[0])
				{
					case 'l':
						return ShiftDirection.Left;
					case 'r':
						return ShiftDirection.Right;
					case 'u':
						return ShiftDirection.Up;
					case 'd':
						return ShiftDirection.Down;
					default:
						return null;
				}
			}
		}

		/// <summary>
		/// Attempts to extract the value of a new card from the specified string.
		/// </summary>
		private static bool TryGetNewCardValue(string str, out int newCardValue)
		{
			if(int.TryParse(str, out newCardValue))
			{
				// Verify that it's a real card.
				return
					newCardValue == 6 ||
					newCardValue == 12 ||
					newCardValue == 24 ||
					newCardValue == 48 ||
					newCardValue == 96 ||
					newCardValue == 192 ||
					newCardValue == 384 ||
					newCardValue == 768 ||
					newCardValue == 1536 ||
					newCardValue == 3072 ||
					newCardValue == 6144;
			}
			else
			{
				return false;
			}
		}
	}
}
