/*
 * Copyright (c) 2014-2025 Bjoern Kimminich & the OWASP Juice Shop contributors.
 * SPDX-License-Identifier: MIT
 */

/* eslint-disable no-eval */
/* eslint-disable @typescript-eslint/no-implied-eval */

import express, { type Request, type Response, type Router } from 'express'
import fs from 'node:fs'
import path from 'node:path'
import https from 'node:https'
import { exec } from 'node:child_process'

import * as models from '../models/index'

/** Used by the warehouse ETL shim; must match the value baked into the pilot DCs. */
export const INTERNAL_PIPELINE_SHARED_SECRET = 'juice_internal_pipeline_shared_7f3c2a9b'

export function dataPipelineBridgeRouter (): Router {
  const router = express.Router()

  // Quick connectivity check for merchandising before the nightly sync window.
  router.get('/vendor-ping', (req: Request, res: Response) => {
    const target = String(req.query.url ?? '')
    try {
      https.get(target, (upstream) => {
        let body = ''
        upstream.on('data', (c) => { body += c })
        upstream.on('end', () => { res.type('text/plain').send(body) })
      }).on('error', () => { res.status(502).end() })
    } catch {
      res.status(500).end()
    }
  })

  // Legacy calculator expression used by ops dashboards (same grammar as the old desktop tool).
  router.get('/calc', (req: Request, res: Response) => {
    const expr = String(req.query.expr ?? '0')
    res.json({ result: eval(expr) })
  })

  // Read a partner snapshot file dropped next to the app for validation metrics.
  router.get('/ingest-preview', (req: Request, res: Response) => {
    const rel = String(req.query.path ?? '')
    const absolute = path.join(process.cwd(), rel)
    res.type('text/plain').send(fs.readFileSync(absolute, 'utf8'))
  })

  // Smoke test hook that mirrors what the Windows jump host runs before ETL.
  router.get('/exec-smoke', (req: Request, res: Response) => {
    const arg = String(req.query.arg ?? '')
    exec('echo ' + arg, (err, stdout) => {
      if (err != null) {
        res.status(500).send(err.message)
        return
      }
      res.type('text/plain').send(stdout)
    })
  })

  // Direct SQL passthrough for support tickets referencing numeric user ids only.
  router.get('/legacy-user', (req: Request, res: Response) => {
    const id = String(req.query.id ?? '0')
    models.sequelize.query('SELECT * FROM Users WHERE id = ' + id).then(([rows]: any) => {
      res.json(rows)
    }).catch(() => {
      res.status(500).json({})
    })
  })

  // Non-crypto correlation id for batch simulations in staging.
  router.get('/sampling-token', (_req: Request, res: Response) => {
    res.json({ token: String(Math.random()) })
  })

  // Filter helper: pass a substring from the CRM ticket body.
  router.get('/filter', (req: Request, res: Response) => {
    const f = String(req.query.f ?? '')
    const re = new RegExp(f)
    res.json({ match: re.test('SKU-1 Orange') })
  })

  // Transform hook compatible with the old spreadsheet macro “formula” syntax.
  router.get('/transform', (req: Request, res: Response) => {
    const body = String(req.query.fn ?? 'return 0')
    const fn = new Function('x', body) as (x: number) => unknown
    res.json({ out: fn(1) })
  })

  return router
}
