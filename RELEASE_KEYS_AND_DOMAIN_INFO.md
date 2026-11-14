# Zombie Rage - Production Release Signing & Domain Information

## 1. Application Identity & Domain
- **Product Name:** Zombie Rage (Display: Zombie Squad / Zombie Rage)
- **Application Identifier (Package ID):** `com.mystudio.zombierage`
- **Company / Studio Name:** `MyStudio`
- **Target Platform:** Android (Google Play Store) & iOS (App Store)
- **Domain:** `mystudio.com` (or `zombierage.mystudio.com`)
- **Digital Asset Links for Deep Links:**
  - Placed at: `https://mystudio.com/.well-known/assetlinks.json`
  - Content: See [unity/Builds/Signing/assetlinks.json](unity/Builds/Signing/assetlinks.json)

---

## 2. Android Production Release Keystore
- **Keystore File Path:** `unity/Builds/Signing/zombierage_release.keystore`
- **Keystore Format:** PKCS12
- **Keystore Password:** `ZombieRage2026!SecureKey`
- **Key Alias:** `zombierage`
- **Key Password:** `ZombieRage2026!SecureKey`
- **Validity:** 10,000 Days (Valid until 2054 - compliant with Google Play requirement)

---

## 3. Cryptographic Fingerprints
Use these for Google Play Console, Firebase, Google Sign-in, and App Links verification:

- **SHA-1 Fingerprint:**
  ```
  FF:40:97:7B:9C:A6:E9:9E:89:FF:7A:F0:A4:95:25:1C:E2:4F:F6:25
  ```

- **SHA-256 Fingerprint (Google Play App Signing):**
  ```
  FF:26:2F:11:7F:A2:A0:3D:93:F8:22:7F:B3:98:4E:38:C2:81:48:E0:0F:FF:68:C0:97:BF:72:EE:0C:9E:85:0D
  ```

---

## 4. Google Play Console Upload Guide
1. Go to [Google Play Console](https://play.google.com/console).
2. Create your Application if you haven't yet:
   - Name: **Zombie Rage**
   - Default language: **English**
   - App or Game: **Game**
   - Free or Paid: **Free** (with In-App Purchases)
3. Navigate to **Production** (or **Closed Testing** / **Open Testing**) -> **Create new release**.
4. Upload the built `.aab` file:
   - `unity/Builds/ZombieRage_v1.0.0_Release.aab` (or Desktop `ZombieSquad_Release.aab`).
5. Fill release notes: `Initial release v1.0.0 with full dynamic levels, battle road, shop, and offline reward mechanics.`
6. Click **Save** and **Review Release**, then **Start rollout to Production**.

