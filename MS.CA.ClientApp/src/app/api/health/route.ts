import { NextResponse } from 'next/server';

// Evaluated per request so the answer reflects whether this process is serving right now,
// which is all a liveness probe can usefully assert.
export const dynamic = 'force-dynamic';

export function GET() {
    return NextResponse.json({ status: 'ok' });
}
