const store = require('../data/store');

const UPGRADE_COSTS = {
  cannonFireRateLevel: (lvl) => lvl * 150,
  startingMobCountLevel: (lvl) => lvl * 100,
  mobSpeedLevel: (lvl) => lvl * 120,
  mobPowerLevel: (lvl) => lvl * 200
};

exports.authenticatePlayer = (req, res) => {
  const { playerId } = req.body;
  const player = store.createOrGetPlayer(playerId);
  return res.json({
    success: true,
    player
  });
};

exports.getProfile = (req, res) => {
  const { playerId } = req.params;
  const player = store.getPlayer(playerId);
  if (!player) {
    return res.status(404).json({ success: false, error: 'Player not found' });
  }
  return res.json({ success: true, player });
};

exports.upgradeStat = (req, res) => {
  const { playerId, upgradeType } = req.body;
  const player = store.getPlayer(playerId);

  if (!player) {
    return res.status(404).json({ success: false, error: 'Player not found' });
  }

  if (!player.upgrades.hasOwnProperty(upgradeType)) {
    return res.status(400).json({ success: false, error: `Invalid upgrade type: ${upgradeType}` });
  }

  const currentLevel = player.upgrades[upgradeType];
  const cost = UPGRADE_COSTS[upgradeType] ? UPGRADE_COSTS[upgradeType](currentLevel) : currentLevel * 100;

  if (player.coins < cost) {
    return res.status(400).json({
      success: false,
      error: `Not enough coins. Required: ${cost}, Available: ${player.coins}`
    });
  }

  player.coins -= cost;
  player.upgrades[upgradeType] += 1;
  store.updatePlayer(player);

  return res.json({
    success: true,
    message: `${upgradeType} upgraded to level ${player.upgrades[upgradeType]}`,
    costPaid: cost,
    player
  });
};
