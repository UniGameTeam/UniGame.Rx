using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;

namespace UniModules.UniGame.Rx.Runtime.Operations
{
    public class BatchPlayerTimingObservable<TSource> : IObservable<TSource>, IObserver<TSource>, IDisposable
    {
        private readonly int _frameCount;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private PlayerLoopTiming _timing = PlayerLoopTiming.Update;
        private bool isActive = false;
        
        private TSource _value;
        private bool _isValueUpdated;
        private Subject<TSource> _observable = new Subject<TSource>();
        private CompositeDisposable _compositeDisposable = new CompositeDisposable();

        public BatchPlayerTimingObservable(IObservable<TSource> source,int frameCount, PlayerLoopTiming timing = PlayerLoopTiming.Update) 
        {
            _frameCount = frameCount;
            _timing = timing;

            source.Subscribe(this).AddTo(_compositeDisposable);
            _compositeDisposable.Add(_observable);
            _compositeDisposable.Add(_tokenSource);
        }

        public void OnNext(TSource source)
        {
            lock (this)
            {
                if (!isActive)
                {
                    isActive = true;
                    UpdateTiming(_timing, _frameCount, _tokenSource);
                }
            }

            _value = source;
            _isValueUpdated = true;
        }
        
        public void OnError(Exception error)
        {
            _observable.OnError(error);
            Dispose();
        }

        public void OnCompleted()
        {
            _observable.OnCompleted();
            Dispose();
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            return _observable.Subscribe(observer);
        }

        public void Dispose()
        {
            _tokenSource.Cancel(false);
            _compositeDisposable.Dispose();
        }

        private async UniTask UpdateTiming(PlayerLoopTiming loopTiming,int awaitAmount,CancellationTokenSource source)
        {
            isActive = true;
            
            var count = 0;
            
            while (!source.IsCancellationRequested && count < awaitAmount)
            {
                await UniTask.Yield(loopTiming);
                count++;
            }
            
            _observable.OnNext(_value);
            _isValueUpdated = false;
            
            isActive = false;
        }
    }

}