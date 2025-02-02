SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `exam`
--

-- --------------------------------------------------------

--
-- Table structure for table `ameliorations`
--

CREATE TABLE `ameliorations` (
  `id` int(11) NOT NULL,
  `correction_id` int(11) DEFAULT NULL,
  `objectifs` text DEFAULT NULL,
  `exercices_suggeres` text DEFAULT NULL,
  `ressources_recommandees` text DEFAULT NULL,
  `methodologie` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- --------------------------------------------------------

--
-- Table structure for table `corrections`
--

CREATE TABLE `corrections` (
  `id` int(11) NOT NULL,
  `devoir_id` int(11) NOT NULL,
  `note` decimal(4,2) NOT NULL,
  `appreciation` text NOT NULL,
  `points_forts` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`points_forts`)),
  `points_ameliorer` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`points_ameliorer`)),
  `competences` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`competences`)),
  `copie` text DEFAULT NULL,
  `date_correction` datetime DEFAULT NULL,
  `annotations` longtext NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- --------------------------------------------------------

--
-- Table structure for table `devoirs`
--

CREATE TABLE `devoirs` (
  `id` int(11) NOT NULL,
  `titre` varchar(255) NOT NULL,
  `enonce` text NOT NULL,
  `contenu` text NOT NULL,
  `date_creation` datetime DEFAULT current_timestamp(),
  `bareme` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT '{     "competences": [         {             "id": 1,             "nom": "Compréhension et analyse du sujet",             "criteres": [                 "Identifier les termes clés d\'un sujet",                 "Reformuler le sujet avec ses propres mots",                 "Formuler une problématique pertinente"             ]         },         {             "id": 2,             "nom": "Élaboration d\'un plan structuré",             "criteres": [                 "Organiser ses idées de manière logique",                 "Connaître les différents types de plans",                 "Annoncer clairement son plan dans l\'introduction"             ]         },         {             "id": 3,             "nom": "Rédaction de l\'introduction et de la conclusion",             "criteres": [                 "Rédiger une accroche efficace",                 "Maîtriser les étapes clés de l\'introduction",                 "Synthétiser et ouvrir la réflexion"             ]         },         {             "id": 4,             "nom": "Développement de l\'argumentation",             "criteres": [                 "Construire des paragraphes argumentatifs",                 "Utiliser des exemples pertinents",                 "Intégrer des références"             ]         },         {             "id": 5,             "nom": "Maîtrise de la langue française",             "criteres": [                 "Orthographe et grammaire",                 "Vocabulaire spécifique",                 "Fluidité de la syntaxe"             ]         },         {             "id": 6,             "nom": "Cohérence et cohésion textuelle",             "criteres": [                 "Utiliser des connecteurs logiques",                 "Assurer la cohérence entre les parties",                 "Contribuer à la problématique"             ]         },         {             "id": 7,             "nom": "Esprit critique et réflexion personnelle",             "criteres": [                 "Prise de position argumentée",                 "Évaluer les arguments",                 "Réflexion personnelle"             ]         }     ] }',
  `type` varchar(255) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci;

-- --------------------------------------------------------

--
-- Table structure for table `evaluations_competences`
--

CREATE TABLE `evaluations_competences` (
  `id` int(11) NOT NULL,
  `correction_id` int(11) NOT NULL,
  `nom_competence` varchar(255) NOT NULL,
  `note` decimal(4,2) DEFAULT NULL,
  `analyse` text DEFAULT NULL,
  `points_forts` text DEFAULT NULL,
  `points_ameliorer` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `ameliorations`
--
ALTER TABLE `ameliorations`
  ADD PRIMARY KEY (`id`),
  ADD KEY `correction_id` (`correction_id`);

--
-- Indexes for table `corrections`
--
ALTER TABLE `corrections`
  ADD PRIMARY KEY (`id`),
  ADD KEY `devoir_id` (`devoir_id`);

--
-- Indexes for table `devoirs`
--
ALTER TABLE `devoirs`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `evaluations_competences`
--
ALTER TABLE `evaluations_competences`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_correction_id` (`correction_id`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `ameliorations`
--
ALTER TABLE `ameliorations`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `corrections`
--
ALTER TABLE `corrections`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `devoirs`
--
ALTER TABLE `devoirs`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `evaluations_competences`
--
ALTER TABLE `evaluations_competences`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `ameliorations`
--
ALTER TABLE `ameliorations`
  ADD CONSTRAINT `ameliorations_ibfk_1` FOREIGN KEY (`correction_id`) REFERENCES `corrections` (`id`);

--
-- Constraints for table `corrections`
--
ALTER TABLE `corrections`
  ADD CONSTRAINT `corrections_ibfk_1` FOREIGN KEY (`devoir_id`) REFERENCES `devoirs` (`id`);

--
-- Constraints for table `evaluations_competences`
--
ALTER TABLE `evaluations_competences`
  ADD CONSTRAINT `evaluations_competences_ibfk_1` FOREIGN KEY (`correction_id`) REFERENCES `corrections` (`id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
