'use client';

import { useRef, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { receptionService } from '@/api';
import { describeError, normalizeError, type AppError } from '@/core/errors';
import { translate } from '@/core/i18n/translate';
import { useToast } from '@/shared/components';
import { deliveryKeys } from './deliveryKeys';
import type { Delivery } from '@/features/reception/types';

interface Command {
    nodeId: string;
    send: () => Promise<Delivery>;
}

export function useDeliveryMutations(deliveryId: string) {
    const client = useQueryClient();
    const toast = useToast();
    const [pendingCounts, setPendingCounts] = useState<ReadonlyMap<string, number>>(new Map());
    // The whole delivery shares one queue (`scope` below); a refetch waits for every queued
    // command to settle so it never overwrites the answer of a command still in flight.
    const inFlight = useRef(0);
    const needsRefetch = useRef(false);

    const track = (nodeId: string, delta: number) =>
        setPendingCounts((previous) => {
            const next = new Map(previous);
            const count = (next.get(nodeId) ?? 0) + delta;
            if (count > 0) next.set(nodeId, count);
            else next.delete(nodeId);
            return next;
        });

    const mutation = useMutation({
        // One queue per delivery: the server applies commands in the order the operator issued them.
        scope: { id: `delivery-${deliveryId}` },
        mutationFn: ({ send }: Command) => send()
    });

    const run = async (nodeId: string, send: () => Promise<Delivery>): Promise<AppError | undefined> => {
        track(nodeId, 1);
        inFlight.current += 1;
        try {
            const delivery = await mutation.mutateAsync({ nodeId, send });
            client.setQueryData(deliveryKeys.current, delivery);
            // This answer is the whole delivery, freshly computed by the server: it already
            // supersedes any staleness an earlier failure in the same burst might have left behind.
            needsRefetch.current = false;
            return undefined;
        } catch (cause) {
            const error = normalizeError(cause);
            // A validation error belongs to the field that sent it; an expired session is handled by the guard.
            if (error.kind !== 'validation' && error.kind !== 'unauthorized') {
                toast.error(translate('toast.saveFailed', { reason: describeError(error) }));
                needsRefetch.current = true;
            }
            return error;
        } finally {
            track(nodeId, -1);
            inFlight.current -= 1;
            if (inFlight.current === 0 && needsRefetch.current) {
                needsRefetch.current = false;
                void client.invalidateQueries({ queryKey: deliveryKeys.current });
            }
        }
    };

    return {
        isPending: (nodeId: string) => pendingCounts.has(nodeId),
        togglePallet: (palletId: string, validated: boolean) =>
            run(palletId, () => receptionService.setPalletValidated(deliveryId, palletId, validated)),
        toggleCarton: (cartonId: string, validated: boolean) =>
            run(cartonId, () => receptionService.setCartonValidated(deliveryId, cartonId, validated)),
        toggleProduct: (productId: string, validated: boolean) =>
            run(productId, () => receptionService.setProductValidated(deliveryId, productId, validated)),
        setQuantity: (productId: string, quantity: number) =>
            run(productId, () => receptionService.setReceivedQuantity(deliveryId, productId, quantity))
    };
}
