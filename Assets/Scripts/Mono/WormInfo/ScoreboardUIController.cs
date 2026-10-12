using System.Collections.Generic;
using Components;
using Mono.Menu;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mono.WormInfo
{
    public class ScoreboardUIController : MonoBehaviour
    {
        private UIDocument uiDocument;
        private Label playerScoreLabel;
        private VisualElement botScoresContainer;
        private VisualElement staminaFill;
        private MenuScreen startScreen;
        private MenuScreen pauseScreen;
        private MenuScreen gameOverScreen;
        private Button playAgainButton;
        private bool canRespawn;

        private const int MaxBotRows = 10;
        private const float BotListInterval = 0.25f;

        private EntityQuery playerQuery;
        private EntityQuery deadQuery;
        private EntityQuery botQuery;
        private readonly List<Label> botLabels = new List<Label>();
        private readonly List<(string name, int score)> botScores = new List<(string name, int score)>();
        private float nextBotListUpdate;

        private EntityManager entityManager;
        private bool started;
        private bool paused;

        void Start()
        {
            uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            playerScoreLabel = root.Q<Label>("player-score");
            botScoresContainer = root.Q<VisualElement>("bot-scores-list");

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            playerQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerTagComponent>(),
                ComponentType.ReadOnly<WormInfoComponent>());
            deadQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerDeadComponent>());
            botQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BotTagComponent>(),
                ComponentType.ReadOnly<WormInfoComponent>());

            BuildStaminaBar(root);
            BuildScreens(root);

            // The game waits on the start screen until Play is pressed
            SetSimulationEnabled(false);
            startScreen.SetVisible(true);
        }

        void BuildStaminaBar(VisualElement root)
        {
            var bar = new VisualElement();
            bar.pickingMode = PickingMode.Ignore;
            bar.style.position = Position.Absolute;
            bar.style.bottom = 24;
            bar.style.left = Length.Percent(50);
            bar.style.width = 240;
            bar.style.marginLeft = -120;
            bar.style.height = 12;
            bar.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            bar.style.borderTopLeftRadius = bar.style.borderTopRightRadius =
                bar.style.borderBottomLeftRadius = bar.style.borderBottomRightRadius = 6;
            bar.style.overflow = Overflow.Hidden;

            staminaFill = new VisualElement();
            staminaFill.pickingMode = PickingMode.Ignore;
            staminaFill.style.height = Length.Percent(100);
            staminaFill.style.backgroundColor = new Color(1f, 0.8f, 0.2f);
            bar.Add(staminaFill);

            root.Add(bar);
        }

        void BuildScreens(VisualElement root)
        {
            startScreen = new MenuScreen(root, "Wiggle", "Eat, grow, don't hit other worms");
            startScreen.AddButton("Play", StartGame);
            startScreen.AddButton("Quit", QuitGame);

            pauseScreen = new MenuScreen(root, "Paused");
            pauseScreen.AddButton("Resume", () => SetPaused(false));
            pauseScreen.AddButton("Quit", QuitGame);

            gameOverScreen = new MenuScreen(root, "Game Over");
            playAgainButton = gameOverScreen.AddButton("Play Again", RequestRespawn);
            gameOverScreen.AddButton("Quit", QuitGame);
        }

        void StartGame()
        {
            started = true;
            startScreen.SetVisible(false);
            SetSimulationEnabled(true);
        }

        void SetPaused(bool value)
        {
            paused = value;
            pauseScreen.SetVisible(value);
            SetSimulationEnabled(!value);
        }

        void SetSimulationEnabled(bool enabled)
        {
            var group = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<SimulationSystemGroup>();
            if (group != null)
                group.Enabled = enabled;
        }

        void RequestRespawn()
        {
            if (deadQuery.TryGetSingletonEntity<PlayerDeadComponent>(out var marker))
            {
                var dead = entityManager.GetComponentData<PlayerDeadComponent>(marker);
                dead.RespawnRequested = true;
                entityManager.SetComponentData(marker, dead);
            }
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void HandleInput(bool isDead)
        {
            if (!started)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    StartGame();
                return;
            }

            if (isDead)
            {
                if (canRespawn && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
                    RequestRespawn();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
                SetPaused(!paused);
        }

        void Update()
        {
            UpdateScoreboard();
        }

        void UpdateScoreboard()
        {
            // The player entity is destroyed on death; the dead marker holds the final score
            bool isDead = deadQuery.TryGetSingleton<PlayerDeadComponent>(out var deadState);
            if (isDead)
            {
                gameOverScreen.Detail.text = $"Final score: {deadState.FinalScore}";

                float remaining = Systems.PlayerSpawnSystem.MinGameOverTime - deadState.TimeSinceDeath;
                canRespawn = remaining <= 0f;
                playAgainButton.SetEnabled(canRespawn);
                playAgainButton.text = canRespawn ? "Play Again" : $"Play Again ({Mathf.CeilToInt(remaining)})";
            }
            else if (!playerQuery.IsEmpty)
            {
                var playerEntity = playerQuery.GetSingletonEntity();
                var playerInfo = entityManager.GetComponentData<WormInfoComponent>(playerEntity);
                playerScoreLabel.text = $"{playerInfo.Name}: {playerInfo.CurrentScore}";

                if (entityManager.HasComponent<WormBoostComponent>(playerEntity))
                {
                    var boost = entityManager.GetComponentData<WormBoostComponent>(playerEntity);
                    staminaFill.style.width = Length.Percent(100f * boost.Stamina / boost.MaxStamina);
                    staminaFill.style.backgroundColor = boost.Exhausted
                        ? new Color(0.8f, 0.3f, 0.2f)
                        : new Color(1f, 0.8f, 0.2f);
                }
            }

            gameOverScreen.SetVisible(isDead);
            HandleInput(isDead);

            if (Time.unscaledTime >= nextBotListUpdate)
            {
                nextBotListUpdate = Time.unscaledTime + BotListInterval;
                UpdateBotList();
            }
        }

        void UpdateBotList()
        {
            botScores.Clear();

            if (!botQuery.IsEmpty)
            {
                var infos = botQuery.ToComponentDataArray<WormInfoComponent>(Allocator.Temp);
                for (int i = 0; i < infos.Length; i++)
                    botScores.Add((infos[i].Name.ToString(), infos[i].CurrentScore));
                infos.Dispose();

                botScores.Sort((a, b) => b.score.CompareTo(a.score));
            }

            int rows = Mathf.Min(botScores.Count, MaxBotRows);

            while (botLabels.Count < rows)
            {
                var botLabel = new Label();
                botLabel.style.fontSize = 16;
                botLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
                botLabel.style.marginBottom = 3;
                botLabels.Add(botLabel);
                botScoresContainer.Add(botLabel);
            }

            for (int i = 0; i < botLabels.Count; i++)
            {
                bool used = i < rows;
                botLabels[i].style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (used)
                    botLabels[i].text = $"{i + 1}. {botScores[i].name}: {botScores[i].score}";
            }
        }

        void OnDestroy()
        {
            playerQuery.Dispose();
            deadQuery.Dispose();
            botQuery.Dispose();
        }
    }
}
