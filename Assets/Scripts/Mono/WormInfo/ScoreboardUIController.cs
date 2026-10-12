using System.Collections.Generic;
using System.Linq;
using Components;
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
        private VisualElement gameOverOverlay;
        private Label gameOverScoreLabel;

        private EntityManager entityManager;

        void Start()
        {
            uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            playerScoreLabel = root.Q<Label>("player-score");
            botScoresContainer = root.Q<VisualElement>("bot-scores-list");

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            BuildGameOverOverlay(root);
        }

        void BuildGameOverOverlay(VisualElement root)
        {
            gameOverOverlay = new VisualElement();
            gameOverOverlay.pickingMode = PickingMode.Ignore;
            gameOverOverlay.style.position = Position.Absolute;
            gameOverOverlay.style.left = 0;
            gameOverOverlay.style.right = 0;
            gameOverOverlay.style.top = 0;
            gameOverOverlay.style.bottom = 0;
            gameOverOverlay.style.alignItems = Align.Center;
            gameOverOverlay.style.justifyContent = Justify.Center;
            gameOverOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            gameOverOverlay.style.display = DisplayStyle.None;

            var title = new Label("Game Over");
            title.style.fontSize = 48;
            title.style.color = Color.white;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;

            gameOverScoreLabel = new Label();
            gameOverScoreLabel.style.fontSize = 24;
            gameOverScoreLabel.style.color = Color.white;
            gameOverScoreLabel.style.marginTop = 10;

            var hint = new Label("Click or press Space to respawn");
            hint.style.fontSize = 18;
            hint.style.color = new Color(0.8f, 0.8f, 0.8f);
            hint.style.marginTop = 20;

            gameOverOverlay.Add(title);
            gameOverOverlay.Add(gameOverScoreLabel);
            gameOverOverlay.Add(hint);
            root.Add(gameOverOverlay);
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

            if (!playerQuery.IsEmpty)
            {
                var playerEntity = playerQuery.GetSingletonEntity();
                var playerInfo = entityManager.GetComponentData<WormInfoComponent>(playerEntity);
                playerScoreLabel.text = $"{playerInfo.Name}: {playerInfo.CurrentScore}";

                bool isDead = entityManager.HasComponent<PlayerDeadComponent>(playerEntity);
                gameOverOverlay.style.display = isDead ? DisplayStyle.Flex : DisplayStyle.None;
                if (isDead)
                    gameOverScoreLabel.text = $"Final score: {playerInfo.CurrentScore}";
            }

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