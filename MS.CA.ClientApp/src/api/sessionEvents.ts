type Listener = () => void;

const listeners = new Set<Listener>();

export const sessionEvents = {
    onEnded(listener: Listener): () => void {
        listeners.add(listener);
        return () => {
            listeners.delete(listener);
        };
    },

    emitEnded(): void {
        for (const listener of [...listeners]) listener();
    }
};
