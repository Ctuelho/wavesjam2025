// WavesManager.cs
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;

public class WavesManager : MonoBehaviour
{
    public ObserversManager ObserversManager;

    public static int GridSize;

    public GameObject WavePrefab;
    public GameObject SlotPrefab;
    public float WaveSize = 1f;
    public float WaveDelay = 0.2f;
    public Camera TargetCamera;
    public float CameraTweenSpeed = 5f;
    public Node[,] Graph;

    private int _gridX, _gridY;
    private float targetOrthoSize;
    private Vector3 targetCameraPosition;
    private bool running = false;

    private GameObject frameObject;
    private List<Slot> slots = new List<Slot>();
    // private const float GRID_SHIFT_X = 2f; // Removido

    [Header("Left Side Sprite")]
    public SpriteRenderer LeftSpriteRenderer;
    public float LeftSpriteSize = 3f;
    public float ScreenEdgePadding = 0.5f;

    [System.Serializable]
    public class Influence { public object source; public float value; }

    [System.Serializable]
    public class Node
    {
        public Wave wave;
        public int X, Y;

        [Header("Neighbors")]
        public Wave TopLeft = null;
        public Wave TopUp = null;
        public Wave TopRight = null;
        public Wave Left = null;
        public Wave Right = null;
        public Wave BottomLeft = null;
        public Wave Bottom = null;
        public Wave BottomRight = null;

        public List<Wave> Neighbors
        {
            get
            {
                if (neighbors == null)
                {
                    neighbors = new List<Wave>() { TopLeft, TopUp, TopRight, Left, Right, BottomLeft, Bottom, BottomRight };
                }
                return neighbors;
            }
        }
        private List<Wave> neighbors = null;

        public float NeighborsInfluence
        {
            get
            {
                float value = 0f;
                foreach (var n in Neighbors)
                {
                    if (n != null)
                    {
                        value += n.Collapse;
                    }
                }
                return value;
            }
        }
    }

    private void OnDisable()
    {
        running = false;
    }

    void LateUpdate()
    {
        if (running)
        {
            if (TargetCamera != null)
            {
                TargetCamera.orthographicSize = Mathf.Lerp(
                    TargetCamera.orthographicSize,
                    targetOrthoSize,
                    Time.deltaTime * CameraTweenSpeed
                );
                TargetCamera.transform.position = Vector3.Lerp(
                    TargetCamera.transform.position,
                    targetCameraPosition,
                    Time.deltaTime * CameraTweenSpeed
                );
            }

            //if (LeftSpriteRenderer != null)
            //{
            //    UpdateLeftSpritePosition();
            //}
        }
    }

    public void CreateGrid(int size)
    {
        CancelInvoke();
        DeleteExistingWaves();

        GridSize = size;
        int x, y;
        x = y = size;
        _gridX = Mathf.Max(1, x);
        _gridY = Mathf.Max(1, y);

        Graph = new Node[_gridX, _gridY];

        float startX = -(_gridX - 1) * WaveSize / 2f; // Ajustado
        float startY = -(_gridY - 1) * WaveSize / 2f;

        for (int i = 0; i < _gridX; i++)
        {
            for (int j = 0; j < _gridY; j++)
            {
                Node node = new Node();
                Wave wave = Instantiate(WavePrefab, transform).GetComponent<Wave>();

                node.wave = wave;
                node.X = i;
                node.Y = j;

                float posX = startX + i * WaveSize;
                float posY = startY + j * WaveSize;

                wave.transform.position = new Vector3(posX, posY, 0);
                wave.transform.localScale = Vector3.one * WaveSize;
                wave.gameObject.name = "Wave " + i + " - " + j;

                Graph[i, j] = node;
            }
        }

        for (int i = 0; i < _gridX; i++)
        {
            for (int j = 0; j < _gridY; j++)
            {
                Node node = Graph[i, j];
                node.TopLeft = (i > 0 && j < _gridY - 1) ? Graph[i - 1, j + 1].wave : null;
                node.TopUp = (j < _gridY - 1) ? Graph[i, j + 1].wave : null;
                node.TopRight = (i < _gridX - 1 && j < _gridY - 1) ? Graph[i + 1, j + 1].wave : null;
                node.Left = (i > 0) ? Graph[i - 1, j].wave : null;
                node.Right = (i < _gridX - 1) ? Graph[i + 1, j].wave : null;
                node.BottomLeft = (i > 0 && j > 0) ? Graph[i - 1, j - 1].wave : null;
                node.Bottom = (j > 0) ? Graph[i, j - 1].wave : null;
                node.BottomRight = (i < _gridX - 1 && j > 0) ? Graph[i + 1, j - 1].wave : null;
            }
        }

        if (ObserversManager != null)
        {
            ObserversManager.Initialize(WaveSize, _gridY);
        }

        CreateFrameAndSlots(_gridX, _gridY, startX, startY);

        //UpdateLeftSpritePosition();

        InvokeRepeating("Collapse", WaveDelay, WaveDelay);
    }

    void DeleteExistingWaves()
    {
        if (Graph != null)
        {
            for (int i = 0; i < _gridX; i++)
            {
                for (int j = 0; j < _gridY; j++)
                {
                    if (Graph[i, j] != null && Graph[i, j].wave != null)
                    {
                        DestroyImmediate(Graph[i, j].wave.gameObject);
                    }
                }
            }
            Graph = null;
        }

        if (frameObject != null)
        {
            DestroyImmediate(frameObject);
            frameObject = null;
        }

        foreach (var slot in slots)
        {
            if (slot != null && slot.gameObject != null)
            {
                DestroyImmediate(slot.gameObject);
            }
        }
        slots.Clear();

        GameObject leftSpriteObject = LeftSpriteRenderer != null ? LeftSpriteRenderer.gameObject : null;
        List<Transform> childrenToDestroy = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.gameObject != leftSpriteObject)
            {
                childrenToDestroy.Add(child);
            }
        }

        foreach (Transform child in childrenToDestroy)
        {
            DestroyImmediate(child.gameObject);
        }
    }

    public void FitCameraToBounds(float totalAreaWidth, float totalAreaCenterX, float totalVisibleHeight)
    {
        if (TargetCamera == null) return;

        float cameraAspect = TargetCamera.aspect;
        float desiredHeight = totalVisibleHeight + WaveSize;
        float desiredWidth = totalAreaWidth + WaveSize;

        float requiredVerticalSizeForHeight = desiredHeight / 2f;
        float requiredVerticalSizeForWidth = desiredWidth / cameraAspect / 2f;

        targetOrthoSize = Mathf.Max(requiredVerticalSizeForHeight, requiredVerticalSizeForWidth);

        targetCameraPosition = new Vector3(totalAreaCenterX, 0, TargetCamera.transform.position.z);
    }

    //public void FitCameraToBounds(float gridMinX, float gridMaxX, float observersMaxX, float totalVisibleHeight)
    //{
    //    if (TargetCamera == null) return;

    //    float cameraAspect = TargetCamera.aspect;
    //    float desiredHeight = totalVisibleHeight + WaveSize;
    //    float totalAreaMinX = gridMinX;
    //    float totalAreaMaxX = observersMaxX;
    //    float totalAreaWidth = totalAreaMaxX - totalAreaMinX;

    //    float desiredWidth = totalAreaWidth + WaveSize;

    //    float requiredVerticalSizeForHeight = desiredHeight / 2f;
    //    float requiredVerticalSizeForWidth = desiredWidth / cameraAspect / 2f;

    //    targetOrthoSize = Mathf.Max(requiredVerticalSizeForHeight, requiredVerticalSizeForWidth);

    //    float totalAreaCenterX = (totalAreaMinX + totalAreaMaxX) / 2f;

    //    targetCameraPosition = new Vector3(totalAreaCenterX, 0, TargetCamera.transform.position.z);
    //}

    public void Collapse()
    {
        if (Graph == null) return;
        int width = Graph.GetLength(0);
        int height = Graph.GetLength(1);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Node node = Graph[i, j];
                node.wave.NeighborsInfluence = new Wave.Influence() { source = node.wave, value = node.NeighborsInfluence };
                node.wave.SlowlyCollapse();
            }
        }
    }

    Slot.DirectionType GetDirection(int gridX, int gridY, int gridWidth, int gridHeight)
    {
        if (gridX == 0 && gridY == gridHeight) return Slot.DirectionType.Diagonal_DownRight;
        if (gridX == gridWidth && gridY == gridHeight) return Slot.DirectionType.Diagonal_DownLeft;
        if (gridX == gridWidth && gridY == 0) return Slot.DirectionType.Diagonal_UpLeft;
        if (gridX == 0 && gridY == 0) return Slot.DirectionType.Diagonal_UpRight;

        if (gridY == gridHeight) return Slot.DirectionType.Down;
        if (gridY == 0) return Slot.DirectionType.Up;
        if (gridX == 0) return Slot.DirectionType.Right;
        if (gridX == gridWidth) return Slot.DirectionType.Left;

        return Slot.DirectionType.None;
    }

    void CreateFrameAndSlots(int x, int y, float startX, float startY)
    {
        running = true;

        float gridWidth = x * WaveSize;
        float gridHeight = y * WaveSize;

        frameObject = new GameObject("Grid Frame");
        frameObject.transform.SetParent(transform);

        float centerGridX = startX + (gridWidth / 2f) - (WaveSize / 2f);
        float centerGridY = startY + (gridHeight / 2f) - (WaveSize / 2f);

        Vector3 framePosition = new Vector3(centerGridX, centerGridY, 0);
        frameObject.transform.position = framePosition;

        float minX = startX - WaveSize;
        float maxX = startX + (x - 1) * WaveSize + WaveSize;
        float minY = startY - WaveSize;
        float maxY = startY + (y - 1) * WaveSize + WaveSize;

        int expandedGridWidth = x + 1;
        int expandedGridHeight = y + 1;

        for (int i = -1; i <= x; i++)
        {
            float currentX;
            int gridX;

            if (i == -1)
            {
                currentX = minX;
                gridX = 0;
            }
            else if (i == x)
            {
                currentX = maxX;
                gridX = expandedGridWidth;
            }
            else
            {
                currentX = startX + i * WaveSize;
                gridX = i + 1;
            }

            if (SlotPrefab != null)
            {
                float currentY = minY;
                int gridY = 0;

                Slot.CornerType corner = Slot.CornerType.None;
                if (i == -1) corner = Slot.CornerType.BottomLeft;
                else if (i == x) corner = Slot.CornerType.BottomRight;

                Slot.DirectionType direction = GetDirection(gridX, gridY, expandedGridWidth, expandedGridHeight);

                Vector3 posB = new Vector3(currentX, currentY, 0);
                GameObject slotBObj = Instantiate(SlotPrefab, transform);
                slotBObj.transform.position = posB;
                slotBObj.transform.localScale = Vector3.one * WaveSize;
                Slot slotB = slotBObj.GetComponent<Slot>();
                if (slotB != null)
                {
                    slotB.Initialize(gridX, gridY, direction, this, corner);
                    slots.Add(slotB);
                }
            }

            if (SlotPrefab != null)
            {
                float currentY = maxY;
                int gridY = expandedGridHeight;

                Slot.CornerType corner = Slot.CornerType.None;
                if (i == -1) corner = Slot.CornerType.TopLeft;
                else if (i == x) corner = Slot.CornerType.TopRight;

                Slot.DirectionType direction = GetDirection(gridX, gridY, expandedGridWidth, expandedGridHeight);

                Vector3 posT = new Vector3(currentX, currentY, 0);
                GameObject slotTObj = Instantiate(SlotPrefab, transform);
                slotTObj.transform.position = posT;
                slotTObj.transform.localScale = Vector3.one * WaveSize;
                Slot slotT = slotTObj.GetComponent<Slot>();
                if (slotT != null)
                {
                    slotT.Initialize(gridX, gridY, direction, this, corner);
                    slots.Add(slotT);
                }
            }
        }

        for (int j = 0; j < y; j++)
        {
            float currentY = startY + j * WaveSize;
            int gridY = j + 1;

            if (SlotPrefab != null)
            {
                float currentX = minX;
                int gridX = 0;

                Slot.DirectionType direction = GetDirection(gridX, gridY, expandedGridWidth, expandedGridHeight);

                Vector3 posL = new Vector3(currentX, currentY, 0);
                GameObject slotLObj = Instantiate(SlotPrefab, transform);
                slotLObj.transform.position = posL;
                slotLObj.transform.localScale = Vector3.one * WaveSize;
                Slot slotL = slotLObj.GetComponent<Slot>();
                if (slotL != null)
                {
                    slotL.Initialize(gridX, gridY, direction, this, Slot.CornerType.None);
                    slots.Add(slotL);
                }
            }

            if (SlotPrefab != null)
            {
                float currentX = maxX;
                int gridX = expandedGridWidth;

                Slot.DirectionType direction = GetDirection(gridX, gridY, expandedGridWidth, expandedGridHeight);

                Vector3 posR = new Vector3(currentX, currentY, 0);
                GameObject slotRObj = Instantiate(SlotPrefab, transform);
                slotRObj.transform.position = posR;
                slotRObj.transform.localScale = Vector3.one * WaveSize;
                Slot slotR = slotRObj.GetComponent<Slot>();
                if (slotR != null)
                {
                    slotR.Initialize(gridX, gridY, direction, this, Slot.CornerType.None);
                    slots.Add(slotR);
                }
            }
        }
    }

    private (int dx, int dy) GetDirectionDelta(Slot.DirectionType direction)
    {
        switch (direction)
        {
            case Slot.DirectionType.Up: return (0, 1);
            case Slot.DirectionType.Down: return (0, -1);
            case Slot.DirectionType.Left: return (-1, 0);
            case Slot.DirectionType.Right: return (1, 0);
            case Slot.DirectionType.Diagonal_UpLeft: return (-1, 1);
            case Slot.DirectionType.Diagonal_UpRight: return (1, 1);
            case Slot.DirectionType.Diagonal_DownLeft: return (-1, -1);
            case Slot.DirectionType.Diagonal_DownRight: return (1, -1);
            default: return (0, 0);
        }
    }

    private float CalculateDecayFactor(Observer.DecayType decayType, int waveIndex, int totalWavesAffected)
    {
        if (decayType == Observer.DecayType.DoesNotDecay)
        {
            return 1f;
        }

        if (decayType == Observer.DecayType.Spread)
        {
            if (totalWavesAffected <= 1) return 1f;
            return 1f - ((float)waveIndex / (totalWavesAffected - 1));
        }

        float decayRate;
        switch (decayType)
        {
            case Observer.DecayType.VerySlow: decayRate = 0.1f; break;
            case Observer.DecayType.Slow: decayRate = 0.2f; break;
            case Observer.DecayType.Medium: decayRate = 0.3f; break;
            case Observer.DecayType.Fast: decayRate = 0.4f; break;
            case Observer.DecayType.VeryFast: decayRate = 0.5f; break;
            default: decayRate = 0f; break;
        }

        return Mathf.Max(0f, 1f - (waveIndex * decayRate));
    }


    public void ApplyObserverInfluence(Observer observer, Slot slot)
    {
        if (slot.CurrentObserver != observer || Graph == null) return;

        int observerRange = observer.range;
        Slot.DirectionType direction = slot.Direction;

        int startWaveX = slot.GridX - 1;
        int startWaveY = slot.GridY - 1;

        List<Wave> wavesToAffect = new List<Wave>();

        (int dx, int dy) = GetDirectionDelta(direction);

        if (dx == 0 && dy == 0) return;

        if (observerRange == 0)
        {
            if (dx != 0 && dy != 0)
            {
                int remainingX = (dx > 0) ? _gridX - (startWaveX + 1) : startWaveX;
                int remainingY = (dy > 0) ? _gridY - (startWaveY + 1) : startWaveY;
                observerRange = Mathf.Min(remainingX, remainingY);
            }
            else if (dx != 0)
            {
                observerRange = (dx > 0) ? _gridX - (startWaveX + 1) : startWaveX;
            }
            else
            {
                observerRange = (dy > 0) ? _gridY - (startWaveY + 1) : startWaveY;
            }
        }

        int currentX = startWaveX;
        int currentY = startWaveY;

        for (int i = 0; i < observerRange; i++)
        {
            currentX += dx;
            currentY += dy;

            if (currentX >= 0 && currentX < _gridX && currentY >= 0 && currentY < _gridY)
            {
                Wave wave = Graph[currentX, currentY].wave;
                if (wave != null)
                {
                    wavesToAffect.Add(wave);
                }
            }
            else
            {
                break;
            }
        }

        int totalWavesAffected = wavesToAffect.Count;

        for (int i = 0; i < totalWavesAffected; i++)
        {
            Wave wave = wavesToAffect[i];
            float decayFactor = CalculateDecayFactor(observer.decay, i, totalWavesAffected);

            float influenceValue = decayFactor * observer.force;

            Wave.Influence influence = new Wave.Influence() { source = observer, value = influenceValue };
            wave.AddInfluence(influence);
        }
    }

    public void RemoveInfluenceSource(object source)
    {
        if (Graph == null || source == null) return;

        int width = Graph.GetLength(0);
        int height = Graph.GetLength(1);

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                Wave wave = Graph[i, j].wave;
                if (wave != null)
                {
                    wave.RemoveInfluence(source);
                }
            }
        }
    }

    public void HandleObserverDrop(Observer observer, Slot targetSlot)
    {
        if (observer == null || targetSlot == null)
        {
            Debug.LogError("HandleObserverDrop observer or slot null!");
            return;
        }

        if (targetSlot.CurrentObserver != null)
        {
            Observer existingObserver = targetSlot.CurrentObserver;

            targetSlot.RemoveObserver();

            if (ObserversManager != null)
            {
                if (existingObserver.CurrentSlot != null)
                {
                    existingObserver.CurrentSlot.RemoveObserver();
                }
                ObserversManager.ReturnObserver(existingObserver);
            }
        }

        targetSlot.AssignObserver(observer);
    }

    public void RemoveObserverFromSlot(Slot slot)
    {
        if (slot != null)
        {
            slot.RemoveObserver();
        }
    }

    public void UpdateLeftSpritePosition(float gridFrameMinX, float paddingX, Sprite newSprite = null, float newSize = 0f)
    {
        if (LeftSpriteRenderer == null) return;

        if (newSprite != null) LeftSpriteRenderer.sprite = newSprite;
        if (newSize > 0f) LeftSpriteSize = newSize;

        LeftSpriteRenderer.transform.localScale = Vector3.one * LeftSpriteSize;

        // A Grid (WavesManager) está em X=0.
        // gridFrameMinX é o canto esquerdo do frame interno da Grid.
        // O Left Sprite precisa estar à esquerda disso.

        // A borda direita do LS deve tocar o canto esquerdo do Grid Frame, mais o paddingX.
        // Borda Direita do LS = gridFrameMinX - paddingX

        // Posição Central do LS = (Borda Direita do LS) - (LeftSpriteSize / 2)
        float leftSpriteMaxXTarget = gridFrameMinX - paddingX;
        float finalPosX = leftSpriteMaxXTarget - (LeftSpriteSize / 2f);

        // Y do Left Sprite deve estar centralizado com a Grid
        float finalPosY = transform.position.y; // Assumindo WavesManager está em Y=0

        LeftSpriteRenderer.transform.position = new Vector3(finalPosX, finalPosY, 0);
    }

    // Função para o LevelManager calcular o offset da Grid
    public float GetGridStartX(float gridMinXLocal)
    {
        // gridMinXLocal é a coordenada X do canto esquerdo da Wave (i=0) se fosse centrada em X=0.
        // O valor é - (GridSize - 1) * WaveSize / 2f.

        // O LevelManager deve calcular o offset, não o WavesManager.
        return 0f;
    }
}