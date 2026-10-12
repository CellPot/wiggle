using System.Collections.Generic;
using System.Linq;
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
            gameOverScreen.AddButton("Play Again", RequestRespawn);
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
            var query = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadWrite<PlayerDeadComponent>() },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });

            if (query.TryGetSingletonEntity<PlayerDeadComponent>(out var player))
            {
                var dead = entityManager.GetComponentData<PlayerDeadComponent>(player);
                dead.RespawnRequested = true;
                entityManager.SetComponentData(player, dead);
            }

            query.Dispose();
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
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
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
            // Get player score
            // Include disabled entities: a dead player is disabled until it respawns
            var playerQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<PlayerTagComponent>(),
                    ComponentType.ReadOnly<WormInfoComponent>()
                },
                Options = EntityQueryOptions.IncludeDisabledEntities
            });

            bool isDead = false;
            if (!playerQuery.IsEmpty)
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

                isDead = entityManager.HasComponent<PlayerDeadComponent>(playerEntity);
                if (isDead)
                    gameOverScreen.Detail.text = $"Final score: {playerInfo.CurrentScore}";
            }

            gameOverScreen.SetVisible(isDead);
            HandleInput(isDead);

            // Get bot scores
            var botQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BotTagComponent>(),
                ComponentType.ReadOnly<WormInfoComponent>()
            );

            if (!botQuery.IsEmpty)
            {
                var entities = botQuery.ToEntityArray(Allocator.Temp);
                var infos = botQuery.ToComponentDataArray<WormInfoComponent>(Allocator.Temp);

                // Create list of bot scores
                var botScores = new List<(string name, int score)>();
                for (int i = 0; i < infos.Length; i++)
                {
                    botScores.Add((infos[i].Name.ToString(), infos[i].CurrentScore));
                }

                // Sort by score descending and take top 10
                var topBots = botScores.OrderByDescending(b => b.score).Take(10).ToList();

                // Update UI
                botScoresContainer.Clear();
                for (int i = 0; i < topBots.Count; i++)
                {
                    var botLabel = new Label($"{i + 1}. {topBots[i].name}: {topBots[i].score}");
                    botLabel.style.fontSize = 16;
                    botLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
                    botLabel.style.marginBottom = 3;
                    botScoresContainer.Add(botLabel);
                }

                entities.Dispose();
                infos.Dispose();
            }

            playerQuery.Dispose();
            botQuery.Dispose();
        }
    }
}