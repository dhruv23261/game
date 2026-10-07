const fs = require('fs');
const path = require('path');
const { v4: uuidv4 } = require('uuid');

const DATA_FILE = path.join(__dirname, 'players.json');

class DataStore {
  constructor() {
    this.players = new Map();
    this.loadData();
  }

  loadData() {
    try {
      if (fs.existsSync(DATA_FILE)) {
        const raw = fs.readFileSync(DATA_FILE, 'utf-8');
        const list = JSON.parse(raw);
        for (const p of list) {
          this.players.set(p.playerId, p);
        }
        console.log(`[Store] Loaded ${this.players.size} players from disk.`);
      }
    } catch (err) {
      console.error('[Store] Error loading players:', err.message);
    }
  }

  saveData() {
    try {
      const list = Array.from(this.players.values());
      fs.writeFileSync(DATA_FILE, JSON.stringify(list, null, 2), 'utf-8');
    } catch (err) {
      console.error('[Store] Error saving players:', err.message);
    }
  }

  createOrGetPlayer(playerId) {
    if (playerId && this.players.has(playerId)) {
      return this.players.get(playerId);
    }

    const newId = playerId || `guest_${uuidv4().substring(0, 8)}`;
    const newPlayer = {
      playerId: newId,
      createdAt: new Date().toISOString(),
      currentLevel: 1,
      maxLevelReached: 1,
      coins: 200,
      gems: 10,
      stats: {
        totalWins: 0,
        totalLosses: 0,
        mobsSpawned: 0,
        adsWatched: 0
      },
      upgrades: {
        cannonFireRateLevel: 1, // reduces fire cooldown
        startingMobCountLevel: 1, // extra initial spawn
        mobSpeedLevel: 1, // mob move speed
        mobPowerLevel: 1 // mob damage
      }
    };

    this.players.set(newId, newPlayer);
    this.saveData();
    return newPlayer;
  }

  getPlayer(playerId) {
    return this.players.get(playerId) || null;
  }

  updatePlayer(player) {
    this.players.set(player.playerId, player);
    this.saveData();
    return player;
  }
}

module.exports = new DataStore();
