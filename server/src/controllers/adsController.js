const store = require('../data/store');
const levels = require('../data/levels.json');

exports.getAdSettings = (req, res) => {
  return res.json({
    success: true,
    adsEnabled: true,
    interstitialIntervalMatches: 1, // Show ad after match if configured
    rewardedMultiplier: 3,
    rewardedFreeCoinsAmount: 150,
    networkProviders: ['UnityAds', 'AdMob', 'AppLovin']
  });
};

exports.claimRewardedMultiplier = (req, res) => {
  const { playerId, levelId } = req.body;
  const player = store.getPlayer(playerId);

  if (!player) {
    return res.status(404).json({ success: false, error: 'Player not found' });
  }

  const level = levels.find(l => l.id === parseInt(levelId, 10));
  if (!level) {
    return res.status(404).json({ success: false, error: 'Level not found' });
  }

  // Base coins was already credited once in completeLevel.
  // 3x multiplier means adding (multiplier - 1) * baseCoins.
  const multiplier = level.adConfig.rewardedMultiplier || 3;
  const bonusCoins = level.rewards.coins * (multiplier - 1);

  player.coins += bonusCoins;
  player.stats.adsWatched += 1;
  store.updatePlayer(player);

  return res.json({
    success: true,
    message: `Rewarded ad verified! Added ${bonusCoins} bonus coins (${multiplier}x total).`,
    bonusCoins,
    player
  });
};

exports.claimFreeReward = (req, res) => {
  const { playerId } = req.body;
  const player = store.getPlayer(playerId);

  if (!player) {
    return res.status(404).json({ success: false, error: 'Player not found' });
  }

  const freeCoins = 150;
  player.coins += freeCoins;
  player.stats.adsWatched += 1;
  store.updatePlayer(player);

  return res.json({
    success: true,
    message: `Free ad reward claimed: +${freeCoins} coins!`,
    bonusCoins: freeCoins,
    player
  });
};
