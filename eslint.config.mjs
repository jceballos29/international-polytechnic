import nx from '@nx/eslint-plugin';

export default [
	...nx.configs['flat/base'],
	...nx.configs['flat/typescript'],
	...nx.configs['flat/javascript'],
	{
		ignores: ['**/dist', '**/out-tsc'],
	},
	{
		files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
		rules: {
			'@nx/enforce-module-boundaries': [
				'error',
				{
					enforceBuildableLibDependency: true,
					allow: ['^.*/eslint(\\.base)?\\.config\\.[cm]?[jt]s$'],
					depConstraints: [
						// Los backends no pueden depender de frontends
						{
							sourceTag: 'type:backend',
							onlyDependOnLibsWithTags: ['type:backend'],
						},
						// Identity solo depende de sí mismo
						{
							sourceTag: 'scope:identity',
							onlyDependOnLibsWithTags: ['scope:identity'],
						},
						// Admin-panel depende de su scope + identity (SSO)
						{
							sourceTag: 'scope:admin-panel',
							onlyDependOnLibsWithTags: [
								'scope:admin-panel',
								'scope:identity',
							],
						},
						// Portal depende de su scope + identity (SSO)
						{
							sourceTag: 'scope:portal',
							onlyDependOnLibsWithTags: [
								'scope:portal',
								'scope:identity',
							],
						},
						// Gradus depende de su scope + identity + portal
						{
							sourceTag: 'scope:gradus',
							onlyDependOnLibsWithTags: [
								'scope:gradus',
								'scope:identity',
								'scope:portal',
							],
						},
					],
				},
			],
		},
	},
	{
		files: [
			'**/*.ts',
			'**/*.tsx',
			'**/*.cts',
			'**/*.mts',
			'**/*.js',
			'**/*.jsx',
			'**/*.cjs',
			'**/*.mjs',
		],
		// Override or add rules here
		rules: {},
	},
];
