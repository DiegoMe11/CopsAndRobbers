using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    List<Tile> robSelectable = new List<Tile>();
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        List<List<int>> adjacency = new List<List<int>>();

        // Inicializar la lista de adyacencia para cada casilla
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            adjacency.Add(new List<int>());
        }

        // Rellenar la lista de adyacencia para cada casilla
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            int row = i / 8;
            int col = i % 8;

            // Agregar la casilla de arriba a la lista de adyacencia
            if (row > 0)
            {
                int aboveIndex = (row - 1) * 8 + col;
                adjacency[i].Add(aboveIndex);
            }

            // Agregar la casilla de abajo a la lista de adyacencia
            if (row < 7)
            {
                int belowIndex = (row + 1) * 8 + col;
                adjacency[i].Add(belowIndex);
            }

            // Agregar la casilla de la izquierda a la lista de adyacencia
            if (col > 0)
            {
                int leftIndex = row * 8 + (col - 1);
                adjacency[i].Add(leftIndex);
            }

            // Agregar la casilla de la derecha a la lista de adyacencia
            if (col < 7)
            {
                int rightIndex = row * 8 + (col + 1);
                adjacency[i].Add(rightIndex);
            }
        }

        // Actualizar la lista de adyacencia de cada casilla
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            tiles[i].adjacency = adjacency[i];
        }
    }


    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
        robSelectable.Clear();
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        // Calcular la distancia de cada casilla a los policías y guardar la distancia más grande y la casilla
        Tile newTile = robberMove();

        robber.GetComponent<RobberMove>().MoveToTile(newTile);
        robber.GetComponent<RobberMove>().currentTile = newTile.numTile;
    }


    public Tile robberMove()
    {
        float distancia1 = 0f;
        float distancia2 = 0f;
        Tile lejano = null;
        float distancia12;
        float distancia22;

        foreach (Tile tile in tiles)
        {
            if (tile.selectable)
            {
                distancia12 = Vector3.Distance(tile.transform.position, cops[0].transform.position);
                distancia22 = Vector3.Distance(tile.transform.position, cops[1].transform.position);

                if (distancia12 > distancia1 && distancia22 > distancia2)
                {
                    distancia1 = distancia12;
                    distancia2 = distancia22;

                    lejano = tile;
                }
            }
        }

        return lejano;
    }


    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;

    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int anothercop = 1;
        int indexcurrentTile;

        // Verifica si el personaje actual es un policía o un ladrón
        if (cop == true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        // Si el policía clicado es el índice 1, establece anothercop en 0
        if (clickedCop == 1)
        {
            anothercop = 0;
        }
        int indexanothercop = cops[anothercop].GetComponent<CopMove>().currentTile;

        // Marca la casilla actual como seleccionada (color rosa)
        tiles[indexcurrentTile].current = true;

        // Crea una cola para el algoritmo BFS (Breadth-First Search)
        Queue<Tile> nodes = new Queue<Tile>();

        // Lista para almacenar las casillas seleccionables
        List<Tile> selectable = new List<Tile>();

        // Agrega la casilla actual a la cola para iniciar el BFS
        nodes.Enqueue(tiles[indexcurrentTile]);

        // Comienza el BFS para encontrar las casillas seleccionables
        while (nodes.Count > 0)
        {
            Tile tile = nodes.Dequeue();

            // Si la casilla ya está en la lista de seleccionables, continúa con la siguiente iteración
            if (selectable.Contains(tile))
            {
                continue;
            }
            else
            {
                // Verifica si la casilla no es la casilla del otro policía y la agrega a la lista de seleccionables
                if (tile != tiles[indexanothercop])
                {
                    selectable.Add(tile);
                }
                else
                {
                    continue;
                }
            }

            // Recorre las casillas adyacentes a la casilla actual y las agrega a la cola si aún no se han visitado
            foreach (var i in tiles[indexcurrentTile].adjacency)
            {
                if (tiles[i] != tiles[indexanothercop])
                {
                    if (!selectable.Contains(tiles[i]))
                    {
                        nodes.Enqueue(tiles[i]);
                    }
                    // Recorre las casillas adyacentes a las casillas adyacentes y las agrega a la cola si aún no se han visitado
                    foreach (var x in tiles[i].adjacency)
                    {
                        if (!selectable.Contains(tiles[x]))
                        {
                            nodes.Enqueue(tiles[x]);
                        }
                    }
                }
            }
        }

        // Marca las casillas seleccionables como seleccionables para el movimiento del policía o ladrón
        if (cop == true)
        {
            foreach (var tile in selectable)
            {
                if (tile != tiles[indexcurrentTile])
                {
                    tile.selectable = true;
                }
            }
        }
        else
        {
            foreach (var tile in selectable)
            {
                if (tile != tiles[indexcurrentTile])
                {
                    tile.selectable = true;
                    robSelectable.Add(tile);
                }
            }
        }
    }

}
