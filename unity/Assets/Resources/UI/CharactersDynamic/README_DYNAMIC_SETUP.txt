ZOMBIE SQUAD CHARACTERS SCREEN ASSET PACK

Use the separate assets to assemble the live screen; do not use the full screenshot as the live UI.
Create runtime text objects for coin balance, diamond balance, character name, class, coin price, gem price and button labels. Do not bake changing values into image files.

Suggested Unity hierarchy:
Canvas > CharactersScreen > Background
Header > BackButton, PageTitle (TMP), CoinBar (Icon + Balance TMP + Plus), DiamondBar (Icon + Balance TMP + Plus)
MainShowcase > ShowcaseFrame, SelectedCharacter, LeftArrow, RightArrow
CharacterGrid > CharacterCard instances > CardFrame, Portrait, ActionButton, ActionLabel (TMP)
BottomDecor

PNG character/logo/background art is raster cropped from the generated reference and may include surrounding glow/background pixels. SVG frame/button assets are editable vector shapes. Character art is not a rigged 3D model.
