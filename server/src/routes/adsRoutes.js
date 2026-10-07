const express = require('express');
const router = express.Router();
const adsController = require('../controllers/adsController');

router.get('/settings', adsController.getAdSettings);
router.post('/claim-multiplier', adsController.claimRewardedMultiplier);
router.post('/claim-free', adsController.claimFreeReward);

module.exports = router;
