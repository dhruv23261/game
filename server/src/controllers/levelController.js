const levels = require('../data/levels.json');
const store = require('../data/store');

exports.getAllLevels = (req, res) => {
  // Return summary of all 15 levels
  const summary = levels.map(l => ({
    id: l.id,
    name: l.name,
    difficulty: l.difficulty,
    rewards: l.rewards,
    adConfig: l.adConfig
  }));

  return res.json({
    success: true,
    totalLevels: levels.length,
    levels: summary
  });
};

exports.getLevelById = (req, res) => {
  const levelId = parseInt(req.params.id, 10);
  const level = levels.find(l => l.id === levelId);

  if (!level) {
    return res.status(404).json({ success: false, error: `Level ${levelId} not found` });
  }

  return res.json({
    success: true,
    level
  });
};

exports.completeLevel = (req, res) => {
  const { playerId, levelId, isVictory, mobsSpawned } = req.body;
  const player = store.getPlayer(playerId);

  if (!player) {
    return res.status(404).json({ success: false, error: 'Player not found' });
  }

  const level = levels.find(l => l.id === parseInt(levelId, 10));
  if (!level) {
    return res.status(404).json({ success: false, error: 'Level not found' });
  }

  if (mobsSpawned) {
    player.stats.mobsSpawned += mobsSpawned;
  }

  if (isVictory) {
    player.stats.totalWins += 1;
    const baseCoins = level.rewards.coins;
    const baseGems = level.rewards.gems;

    player.coins += baseCoins;
    player.gems += baseGems;

    if (level.id >= player.maxLevelReached && level.id < 15) {
      player.maxLevelReached = level.id + 1;
      player.currentLevel = level.id + 1;
    }

    store.updatePlayer(player);

    return res.json({
      success: true,
      result: 'victory',
      earnedCoins: baseCoins,
      earnedGems: baseGems,
      shouldShowInterstitial: level.adConfig.showInterstitialOnFinish,
      rewardedMultiplierAvailable: level.adConfig.rewardedMultiplier,
      player
    });
  } else {
    player.stats.totalLosses += 1;
    store.updatePlayer(player);

    return res.json({
      success: true,
      result: 'defeat',
      earnedCoins: 0,
      earnedGems: 0,
      shouldShowInterstitial: level.adConfig.showInterstitialOnFinish,
      player
    });
  }
};
