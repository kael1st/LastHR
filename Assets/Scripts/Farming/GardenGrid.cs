using UnityEngine;

namespace FarmingSystem
{
    /// <summary>
    /// Generates and arranges a grid of GardenPlot tiles in the scene.
    /// </summary>
    public class GardenGrid : MonoBehaviour
    {
        [Header("Grid Dimensions")]
        [SerializeField] private int columns = 4;
        [SerializeField] private int rows = 3;
        [SerializeField] private Vector2 cellSize = new Vector2(1.1f, 1.1f);

        [Header("Plot Prefab / Assets")]
        [SerializeField] private GameObject plotPrefab;
        [SerializeField] private Sprite untilledSprite;
        [SerializeField] private Sprite tilledDrySprite;
        [SerializeField] private Sprite tilledWateredSprite;
        [SerializeField] private Sprite highlightSprite;

        [Header("Auto-generation")]
        [SerializeField] private bool generateOnStart = false;

        private void Start()
        {
            if (generateOnStart && transform.childCount == 0)
            {
                GenerateGrid();
            }
        }

        [ContextMenu("Generate Grid")]
        public void GenerateGrid()
        {
            ClearExistingChildren();

            float startX = -(columns - 1) * cellSize.x * 0.5f;
            float startY = (rows - 1) * cellSize.y * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Vector3 localPos = new Vector3(startX + c * cellSize.x, startY - r * cellSize.y, 0f);
                    CreatePlotAt(localPos, $"Plot_{c}_{r}");
                }
            }
        }

        private void ClearExistingChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        private void CreatePlotAt(Vector3 localPosition, string plotName)
        {
            GameObject plotObj;
            if (plotPrefab != null)
            {
                plotObj = Instantiate(plotPrefab, transform);
                plotObj.name = plotName;
                plotObj.transform.localPosition = localPosition;
            }
            else
            {
                plotObj = new GameObject(plotName);
                plotObj.transform.SetParent(transform, false);
                plotObj.transform.localPosition = localPosition;

                var collider = plotObj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(1f, 1f);
                collider.isTrigger = true;

                var plot = plotObj.AddComponent<GardenPlot>();
                plot.ConfigureSprites(untilledSprite, tilledDrySprite, tilledWateredSprite);
                plot.ConfigureHighlightSprite(highlightSprite);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.4f);
            float startX = -(columns - 1) * cellSize.x * 0.5f;
            float startY = (rows - 1) * cellSize.y * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Vector3 pos = transform.position + new Vector3(startX + c * cellSize.x, startY - r * cellSize.y, 0f);
                    Gizmos.DrawWireCube(pos, new Vector3(cellSize.x * 0.95f, cellSize.y * 0.95f, 0.1f));
                }
            }
        }
    }
}
