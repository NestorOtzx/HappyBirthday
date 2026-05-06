namespace Nivelo.Singletons
{
  /// <summary>
  /// Represents all loadable game scenes.
  /// <para></para>
  /// To add a new scene:
  /// 1. Add the scene asset to Build Settings.
  /// 2. Add a new enum entry with the exact same name as the scene asset.
  /// 3. Do not change existing enum names to avoid breaking references.
  /// </summary>
  public enum GameScenes
  {
    None,
    Forest,
    Combat,
    LoadingCombat,
    LoadingWorld,
    IntroductionScene,
    Credits,
  }
}
