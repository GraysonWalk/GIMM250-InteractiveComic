// INavigationController was removed — Unity's serialization system cannot reference interfaces
// as component fields, making DIP impractical here. NavigationController is self-contained:
// it subscribes directly to ComicManager events and its public methods are called by UI buttons
// and Input Actions. Reintroduce this interface if a second NavigationController implementation
// (e.g. touch, gamepad) is needed.