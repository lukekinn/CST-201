Feel free to make any changes you see fit!

BEGIN BattleShip   //set up the gameboard and ship selection from initial classs

  print "This will be some sort of welcome message to the game"

  CREATE playerBoard //visible to player, sets up their board //each is a 10x10 grid of cells
  CREATE computerBoard //player should have no view of this
  CREATE playerGuesses //visible to player, displays guesses
  CREATE computerGuesses //player should have no view of this

  DEFINE destroyer //2x2 square
  DEFINE submarine //3 diagonal squares (in either direction)
  DEFINE cruiser //3 consecutive squares, vertical or horizontal

  GAME SETUP
    Initualize empty playerBoard, computerBoard, playerGuesses, computerGuesses
    Initualize ship list
    Player ship placement //loop this for each shiip and orientation
    Computer ship placement

  MAIN GAME LOOP
    current = player
      while Ships are not sunk for player and computer
      If current == Player
        loop:
        shot = promptPlayerForShot()
        If outOfBounds(shot) or playerGuessBoard.alreadyTried(shot): show error, continue loop.
        result = applyShot(computerBoard, shot) //returns MISS, HIT, or SUNK(ship)
        playerGuessBoard.mark(shot, result)
        If result == MISS: current = COMPUTER; break inner loop (turn ends)
        If result == HIT or SUNK:
          If all ships are sunk, player wins
          Else let player choose take another shot
      Else (current == Computer)
        shot = computerPickNextShot(computerShotPool) //pick randomly from remaining
        result = applyShot(playerBoard, shot)
        computerGuessBoard.mark(shot, result)
        If result == MISS: current = PLAYER; break inner loop
        If result == HIT or SUNK:
          If allShipsSunk(player): announce COMPUTER wins; exit loop
          else: computer shoots again (continue inner loop)

