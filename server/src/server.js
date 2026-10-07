require('dotenv').config();
const express = require('express');
const cors = require('cors');

const playerRoutes = require('./routes/playerRoutes');
const levelRoutes = require('./routes/levelRoutes');
const adsRoutes = require('./routes/adsRoutes');

const app = express();
const PORT = process.env.PORT || 4000;

app.use(cors());
app.use(express.json());

// Health check
app.get('/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() });
});

// API Routes
app.use('/api/player', playerRoutes);
app.use('/api/levels', levelRoutes);
app.use('/api/ads', adsRoutes);

// Root fallback
app.get('/', (req, res) => {
  res.json({
    message: 'Mob Control Style Game Backend Server is Running!',
    endpoints: {
      health: 'GET /health',
      playerAuth: 'POST /api/player/auth',
      playerProfile: 'GET /api/player/:playerId',
      playerUpgrade: 'POST /api/player/upgrade',
      allLevels: 'GET /api/levels',
      levelById: 'GET /api/levels/:id',
      completeLevel: 'POST /api/levels/complete',
      adsSettings: 'GET /api/ads/settings',
      claimMultiplier: 'POST /api/ads/claim-multiplier',
      claimFreeReward: 'POST /api/ads/claim-free'
    }
  });
});

app.listen(PORT, () => {
  console.log(`=========================================`);
  console.log(`🚀 Game Server is running on port ${PORT}`);
  console.log(`👉 http://localhost:${PORT}`);
  console.log(`=========================================`);
});
