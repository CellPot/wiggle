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

        private EntityManager entityManager;

        void Start()
        {
            uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            playerScoreLabel = root.Q<Label>("player-score");
            botScoresContainer = root.Q<VisualElement>("bot-scores-list");

            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        void Update()
        {
            UpdateScoreboard();
        }

        void UpdateScoreboard()
        {
            // Get player score
            var playerQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<PlayerTagComponent>(),
                ComponentType.ReadOnly<WormInfoComponent>()
            );

            if (!playerQuery.IsEmpty)
            {
                var playerEntity = playerQuery.GetSingletonEntity();
                var playerInfo = entityManager.GetComponentData<WormInfoComponent>(playerEntity);
                playerScoreLabel.text = $"{playerInfo.Name}: {playerInfo.CurrentScore}";
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