using System;
using Gokoukotori.PoseTune;

namespace Gokoukotori.PoseTune.Editor
{
    internal sealed class PoseTuneRootSnapshotCache<TSnapshot> : IDisposable
    {
        private readonly Func<PoseTuneRoot, TSnapshot> _createSnapshot;
        private readonly PoseTuneAssistantScopedInvalidation _invalidation;
        private PoseTuneRoot _cachedRoot;
        private TSnapshot _snapshot;
        private bool _cacheValid;

        public PoseTuneRootSnapshotCache(Func<PoseTuneRoot, TSnapshot> createSnapshot)
        {
            _createSnapshot = createSnapshot ?? throw new ArgumentNullException(nameof(createSnapshot));
            _invalidation = new PoseTuneAssistantScopedInvalidation(Invalidate);
        }

        public void Invalidate()
        {
            _cacheValid = false;
        }

        public void Dispose()
        {
            _invalidation.Dispose();
        }

        public TSnapshot Get(PoseTuneRoot root)
        {
            _invalidation.Track(root);
            if (!_cacheValid || _cachedRoot != root)
            {
                _cachedRoot = root;
                _snapshot = _createSnapshot(root);
                _cacheValid = true;
            }

            return _snapshot;
        }
    }
}
