import { sessionEvents } from './sessionEvents';

describe('sessionEvents', () => {
    it('calls a subscriber when the session ends', () => {
        const listener = jest.fn();
        const unsubscribe = sessionEvents.onEnded(listener);

        sessionEvents.emitEnded();

        expect(listener).toHaveBeenCalledTimes(1);
        unsubscribe();
    });

    it('stops calling a subscriber once it unsubscribed', () => {
        const listener = jest.fn();
        sessionEvents.onEnded(listener)();

        sessionEvents.emitEnded();

        expect(listener).not.toHaveBeenCalled();
    });

    it('keeps notifying the others when a listener unsubscribes itself during an emit', () => {
        const calls: string[] = [];
        const unsubscribeFirst = sessionEvents.onEnded(() => {
            calls.push('first');
            unsubscribeFirst();
        });
        const unsubscribeSecond = sessionEvents.onEnded(() => calls.push('second'));

        sessionEvents.emitEnded();
        sessionEvents.emitEnded();

        expect(calls).toEqual(['first', 'second', 'second']);
        unsubscribeSecond();
    });
});
